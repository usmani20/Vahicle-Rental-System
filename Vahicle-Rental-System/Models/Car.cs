using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vahicle_Rental_System.Models
{
    public class Car
    {
        [Key]
        public int Id { get; set; }

        // --- Basic Info ---
        [Required]
        public string Brand { get; set; } // e.g., Honda
        [Required]
        public string Model { get; set; } // e.g., Civic

        public string Description { get; set; }
        public string? ImageUrl { get; set; }

        // --- NEW: 360 Video (Optional) ---
        public string? VideoUrl { get; set; } // Link to YouTube/Video

        // --- NEW: Category Relationship ---
        [Required]
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public Category Category { get; set; }

        // --- NEW: Pricing & Rules (From your Image) ---
        public decimal PriceSelfDrive { get; set; }   // e.g., 8500
        public decimal PriceWithDriver { get; set; }  // e.g., 11000

        public decimal OvertimeRate { get; set; }     // e.g., 350
        public string FuelPolicy { get; set; }        // e.g., "Refill or pay PKR 40/KM"

        public int DriverIncludedHours { get; set; } = 10; // Default 10 hrs
        public int SelfDriveHours { get; set; } = 24;      // Default 24 hrs

        // --- Specs ---
        public string Transmission { get; set; } // Automatic
        public int Seats { get; set; }           // 5
        public bool IsAvailable { get; set; } = true;
    }
}