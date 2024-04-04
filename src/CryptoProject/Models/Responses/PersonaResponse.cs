namespace CryptoProject.Models.Responses
{
    public record PersonaResponse
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get => $"{FirstName} {LastName}"; }

        public string? Role { get; set; }
        public IList<string>? Roles { get; set; }
        public Guid? WalletId { get; set; }
        public decimal? WalletBalance { get; set; }

    }
    public record TokenResult(string Token, DateTime ExpiryDate);

}
