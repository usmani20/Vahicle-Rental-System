using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Vahicle_Rental_System.Models.ViewModels
{
    public class CarViewModel
    {
        public int Id { get; set; }

        [Required]
        public string Brand { get; set; }
        [Required]
        public string Model { get; set; }
        public string Description { get; set; }

        // Pricing
        [Required]
        public decimal PriceSelfDrive { get; set; }
        [Required]
        public decimal PriceWithDriver { get; set; }
        public decimal OvertimeRate { get; set; }
        public string FuelPolicy { get; set; }

        // Specs
        public string Transmission { get; set; }
        public int Seats { get; set; }
        public int CategoryId { get; set; } // Selected Category ID

        // Media
        public IFormFile? CarImage { get; set; }
        public string? ExistingImageUrl { get; set; }

        [Url(ErrorMessage = "Please enter a valid URL")]
        public string? VideoUrl { get; set; } // Optional 360 Video Link

        // Dropdown List for View
        public IEnumerable<SelectListItem>? Categories { get; set; }
    }
}