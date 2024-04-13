using CryptoProject.Entities.Enums;

namespace CryptoProject.Models.Requests
{
    public record TopUpWalletRequest
    {
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public string Pin { get; set; }
        public WalletType FromWalletType { get; set; }
        public WalletType ToWalletType { get; set; }
        public string Otp { get; set; }
    }
}
