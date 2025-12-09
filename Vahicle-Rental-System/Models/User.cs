using System.ComponentModel.DataAnnotations;

namespace Vahicle_Rental_System.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; } // We will store the Hashed password here

        public string? FullName { get; set; }
    }
}