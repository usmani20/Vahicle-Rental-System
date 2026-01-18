using System.ComponentModel.DataAnnotations;

namespace Vahicle_Rental_System.Models
{
    public class EditProfileViewModel
    {
        public string? FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "Password must contain at least one capital letter and one number.")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string? ConfirmPassword { get; set; }
    }
}