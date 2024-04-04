namespace CryptoProject.Models.Requests
{
    public record TransactionRequest
    {
        public Guid ReceiverId { get; set; }
        public decimal Amount { get; set; }
    }
}
