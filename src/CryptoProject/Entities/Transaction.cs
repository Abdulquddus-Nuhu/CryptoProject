using CryptoProject.Entities.Enums;
using CryptoProject.Entities.Identity;

namespace CryptoProject.Entities
{
    public class Transaction : BaseEntity
    {
        public Guid? SenderId { get; set; } // Nullable for admin adjustments
        public Guid? ReceiverId { get; set; } // Nullable for admin adjustments
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public TransactionStatus Status { get; set; }
        public TransactionType Type { get; set; }


        public User? Sender { get; set; } 
        public User? Receiver { get; set; }
    }
}
