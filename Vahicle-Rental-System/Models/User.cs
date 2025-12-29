using System.ComponentModel.DataAnnotations;

namespace Vahicle_Rental_System.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        public required string Email { get; set; }

        [Required]
        public required string Password { get; set; }

        public string? FullName { get; set; }
        public bool IsEmailVerified { get; set; } // Add this property to fix CS0117
    }
}