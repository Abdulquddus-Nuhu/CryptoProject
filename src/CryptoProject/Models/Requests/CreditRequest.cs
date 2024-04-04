namespace CryptoProject.Models.Requests
{
    public record CreditRequest
    {
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
    }
}
