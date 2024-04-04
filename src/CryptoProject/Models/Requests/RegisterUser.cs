using CryptoProject.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace CryptoProject.Models.Requests
{
    public record RegisterUser
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }
        public string? MiddleName { get; set; }


        [Required]
        public string Email { get; set; }
        
        [Required]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Please provide a value for password field"), MinLength(8, ErrorMessage = "Password must consist of at least 8 characters")]
        [StringLength(255)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please provide a value for the confirm password field"), Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
        [StringLength(255)]
        public string ConfirmPassword { get; set; } = string.Empty;

        public AccountType AccountType { get; set; }

        [Required]
        public string Address { get; set; }
        [Required]
        public string City { get; set; }
        [Required]
        public string State { get; set; }
    }
}
