namespace CryptoProject.Models.Responses
{
    public class LoginResponse
    {
        public string Token { get; set; }
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

    }
}
