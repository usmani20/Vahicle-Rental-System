using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Vahicle_Rental_System.Models.ViewModels
{
    public class CarViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Brand is required")]
        public string Brand { get; set; }

        [Required(ErrorMessage = "Model is required")]
        public string Model { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Self Drive Price is required")]
        public int PriceSelfDrive { get; set; }

        [Required(ErrorMessage = "Driver Price is required")]
        public int PriceWithDriver { get; set; }

        public int OvertimeRate { get; set; }
        public string? FuelPolicy { get; set; }
        public string? Transmission { get; set; }
        public int Seats { get; set; }

        // --- NEW FIELDS FOR FORM ---
        public string? Mileage { get; set; }
        public string? Luggage { get; set; }
        public string? FuelType { get; set; }

        [Required(ErrorMessage = "Please select a category")]
        public int CategoryId { get; set; }

        public IFormFile? CarImage { get; set; }
        public string? ExistingImageUrl { get; set; }
        public string? VideoUrl { get; set; }

        [ValidateNever]
        public IEnumerable<SelectListItem>? Categories { get; set; }
    }
}