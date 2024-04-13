using CryptoProject.Entities.Enums;

namespace CryptoProject.Models.Responses
{
    public class TransactionResponse
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;


        public Guid? SenderId { get; set; }
        public Guid? ReceiverId { get; set; }
        public string? Sender { get; set; }
        public string? SenderEmail { get; set; }
        public string? Receiver { get; set; }
        public string? ReceiverEmail { get; set; }
        public string Details { get; set; }
        public string ReceiverWalletAddress { get; set; }
        public string WalletType { get; set; }
        public string CoinType { get; set; }

    }
}
