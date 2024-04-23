using CryptoProject.Entities.Enums;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CryptoProject.Models.Responses
{
    public record UserResponse
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string? MiddleName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; }

        public string FullName { get => $"{FirstName} {LastName}"; }

        public string AccountType { get; set; }

        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public Guid? WalletId { get; set; }
        public decimal? WalletBalance { get; set; }
        public Guid? LedgerAccountId { get; set; }
        public decimal? LedgerAccountBalance { get; set; }
        public Guid? USDAccountId { get; set; }
        public decimal? USDAccountBalance { get; set; }
        public string LedgerAccountNumber { get; set; }
        public string Pin { get; set; }
        public string Country { get; set; }
        public string AccountNumber { get; set; }
        public bool IsActive { get; set; }
        public string Password { get; set; }
    }

}
