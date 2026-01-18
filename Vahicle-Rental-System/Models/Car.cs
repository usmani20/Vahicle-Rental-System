using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vahicle_Rental_System.Models
{
    public class Car
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Brand { get; set; }
        [Required]
        public string Model { get; set; }

        public string Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? VideoUrl { get; set; }

        [Required]
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public Category Category { get; set; }

        public int PriceSelfDrive { get; set; }
        public int PriceWithDriver { get; set; }
        public int OvertimeRate { get; set; }
        public string FuelPolicy { get; set; }

        // --- NEW FEATURES ---
        public string? Mileage { get; set; }   // e.g. "12 km/l"
        public string? Luggage { get; set; }   // e.g. "2 Bags"
        public string? FuelType { get; set; }  // e.g. "Petrol", "Diesel", "Electric"

        public string Transmission { get; set; }
        public int Seats { get; set; }
        public bool IsAvailable { get; set; } = true;
    }
}