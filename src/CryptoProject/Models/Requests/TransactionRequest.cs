using CryptoProject.Entities.Enums;

namespace CryptoProject.Models.Requests
{
    public record TransactionRequest
    {
        public Guid ReceiverId { get; set; }
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public string Details { get; set; }
        public string ReceiverWalletAddress { get; set; }
        public string Pin { get; set; }
        public WalletType WalletType { get; set; }
        public string Otp { get; set; }
    }
}
