using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vahicle_Rental_System.Data;
using Vahicle_Rental_System.Models;
using System.Net.Mail;
using System.Net;

namespace Vahicle_Rental_System.Controllers
{
    public class BookingController : Controller
    {
        private readonly AppDbContext _context;

        public BookingController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(int CarId, DateTime PickupDate, DateTime ReturnDate,
                                                 string RentalType, string PickupLocation, string DropoffLocation)
        {
            // 1. Double Booking Prevention
            bool isTaken = await _context.Bookings.AnyAsync(b =>
                b.CarId == CarId && b.Status == "Confirmed" &&
                ((PickupDate < b.ReturnDate) && (ReturnDate > b.PickupDate)));

            if (isTaken)
            {
                TempData["Error"] = "Vehicle is already booked for these dates. Please choose another timeframe.";
                return RedirectToAction("CarDetails", "Home", new { id = CarId });
            }

            var car = await _context.Cars.FindAsync(CarId);
            if (car == null) return NotFound();

            // 2. Price Calculation (PKR as Integer)
            int days = (int)(ReturnDate - PickupDate).TotalDays;
            if (days <= 0) days = 1;
            int rate = (RentalType == "SelfDrive") ? car.PriceSelfDrive : car.PriceWithDriver;

            // 3. Create Pending Booking with Map Locations
            var booking = new Booking
            {
                CarId = CarId,
                UserEmail = User.Identity?.Name ?? "guest@customer.com",
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
                // Log email failure but don't stop the user's success flow
                Console.WriteLine("Email Error: " + ex.Message);
            }
        }
    }
}