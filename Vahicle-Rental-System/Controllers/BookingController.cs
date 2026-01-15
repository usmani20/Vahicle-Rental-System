using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vahicle_Rental_System.Data;
using Vahicle_Rental_System.Models;
using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace Vahicle_Rental_System.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly AppDbContext _context;

        public BookingController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var userEmail = User.Identity?.Name;

            // Security check: Match ID and UserEmail
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id && b.UserEmail == userEmail);

            if (booking == null)
            {
                return NotFound();
            }

            // Restriction: Only allow deletion if Status is Pending
            if (booking.Status == "Pending")
            {
                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Your pending reservation has been successfully removed from your dashboard.";
            }
            else
            {
                TempData["Error"] = "Security Notice: Confirmed and paid bookings cannot be cancelled via the dashboard. Please contact our support team.";
            }

            return RedirectToAction("MyBookings");
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(int CarId, DateTime PickupDate, DateTime ReturnDate,
                                                 string RentalType, string PickupLocation, string DropoffLocation)
        {
            var userEmail = User.Identity?.Name;

            // 1. Prevent one user from booking the same car twice if they have an active or pending request
            bool alreadyBookedByUser = await _context.Bookings.AnyAsync(b =>
                b.CarId == CarId &&
                b.UserEmail == userEmail &&
                (b.Status == "Confirmed" || b.Status == "Pending"));

            if (alreadyBookedByUser)
            {
                TempData["Error"] = "Our records show you already have a pending or confirmed booking for this car. Please visit your dashboard to manage existing rentals.";
                return RedirectToAction("CarDetails", "Home", new { id = CarId });
            }

            // 2. Standard Availability Check (Overlap with other users)
            bool isTaken = await _context.Bookings.AnyAsync(b =>
                b.CarId == CarId && b.Status == "Confirmed" &&
                ((PickupDate < b.ReturnDate) && (ReturnDate > b.PickupDate)));

            if (isTaken)
            {
                TempData["Error"] = "Unfortunately, this vehicle has just been reserved by another customer for the selected dates. Please try a different timeframe.";
                return RedirectToAction("CarDetails", "Home", new { id = CarId });
            }

            var car = await _context.Cars.FindAsync(CarId);
            if (car == null) return NotFound();

            // 3. Price Calculation (PKR as Integer)
            int days = (int)(ReturnDate - PickupDate).TotalDays;
            if (days <= 0) days = 1;
            int rate = (RentalType == "SelfDrive") ? car.PriceSelfDrive : car.PriceWithDriver;

            // 4. Create Pending Booking with Map Locations
            var booking = new Booking
            {
                CarId = CarId,
                UserEmail = userEmail ?? "guest@customer.com",
                PickupDate = PickupDate,
                ReturnDate = ReturnDate,
                PickupLocation = PickupLocation,
                DropoffLocation = DropoffLocation,
                RentalType = RentalType,
                TotalPrice = rate * days,
                Status = "Pending"
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return RedirectToAction("Payment", new { id = booking.Id });
        }

        public async Task<IActionResult> Payment(int id)
        {
            var booking = await _context.Bookings.Include(b => b.Car).FirstOrDefaultAsync(x => x.Id == id);
            if (booking == null) return NotFound();

            return View(booking);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment(int BookingId, string CardNumber, string Expiry, string CVV)
        {
            var booking = await _context.Bookings.Include(b => b.Car).FirstOrDefaultAsync(x => x.Id == BookingId);
            if (booking == null) return NotFound();

            string cleanCardNumber = CardNumber?.Replace(" ", "") ?? "";

            if (string.IsNullOrEmpty(cleanCardNumber) || cleanCardNumber.Length != 16)
            {
                ViewBag.Error = "Invalid Card Details. Please provide a valid 16-digit card number.";
                return View("Payment", booking);
            }

            // Finalize Booking
            booking.Status = "Confirmed";
            booking.TransactionId = "PKR-TXN-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

            var car = await _context.Cars.FindAsync(booking.CarId);
            if (car != null) car.IsAvailable = false;

            await _context.SaveChangesAsync();
            SendEmail(booking);

            // Redirect to Receipt instead of returning view directly
            return RedirectToAction("Receipt", new { id = booking.Id });
        }

        public async Task<IActionResult> Receipt(int id)
        {
            var booking = await _context.Bookings.Include(b => b.Car).FirstOrDefaultAsync(x => x.Id == id);
            if (booking == null || booking.Status != "Confirmed") return RedirectToAction("Index", "Home");

            return View(booking);
        }

        public async Task<IActionResult> MyBookings()
        {
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Login", "Account");

            var bookings = await _context.Bookings
                .Include(b => b.Car)
                .Where(b => b.UserEmail == userEmail)
                .OrderByDescending(b => b.Id)
                .ToListAsync();

            return View(bookings);
        }

        private void SendEmail(Booking b)
        {
            try
            {
                var smtp = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("carbooooooooook@gmail.com", "nchxasxpbbwvmljx"),
                    EnableSsl = true,
                };

                var msg = new MailMessage("carbooooooooook@gmail.com", b.UserEmail)
                {
                    Subject = "Booking Confirmed - Car Rental System",
                    Body = $@"
                        <div style='font-family: sans-serif; border: 1px solid #eee; padding: 20px;'>
                            <h2 style='color: #01d28e;'>Rental Confirmed!</h2>
                            <p><b>Vehicle:</b> {b.Car.Brand} {b.Car.Model}</p>
                            <p><b>Pick-up Location:</b> {b.PickupLocation}</p>
                            <p><b>Drop-off Location:</b> {b.DropoffLocation}</p>
                            <p><b>Dates:</b> {b.PickupDate.ToShortDateString()} to {b.ReturnDate.ToShortDateString()}</p>
                            <hr/>
                            <h3>Total Paid: PKR {b.TotalPrice.ToString("N0")}</h3>
                            <p><small>Transaction ID: {b.TransactionId}</small></p>
                        </div>",
                    IsBodyHtml = true
                };
                smtp.Send(msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Email Error: " + ex.Message);
            }
        }
    }
}