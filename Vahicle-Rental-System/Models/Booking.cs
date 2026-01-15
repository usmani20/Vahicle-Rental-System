using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vahicle_Rental_System.Models
{
    public class Booking
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CarId { get; set; }
        [ForeignKey("CarId")]
        public Car Car { get; set; }

        [Required]
        public string UserEmail { get; set; }

        [Required]
        public DateTime PickupDate { get; set; }

        [Required]
        public DateTime ReturnDate { get; set; }

        [Required]
        public string RentalType { get; set; } // "SelfDrive" or "WithDriver"

        // Using int for PKR as requested
        public int TotalPrice { get; set; }

        // Status: "Pending", "Confirmed", "Rented", "Completed"
        public string Status { get; set; } = "Pending";

        [Required]
        public string PickupLocation { get; set; }

        [Required]
        public string DropoffLocation { get; set; }

        public string? TransactionId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}