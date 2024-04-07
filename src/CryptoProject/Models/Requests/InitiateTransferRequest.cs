namespace CryptoProject.Models.Requests
{
    public record InitiateTransferRequest
    {
        public Guid UserId { get; set; }
    }
}
