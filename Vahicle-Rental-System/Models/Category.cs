using System.ComponentModel.DataAnnotations;

namespace Vahicle_Rental_System.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } // e.g., "SUV", "Luxury", "Standard"
    }
}