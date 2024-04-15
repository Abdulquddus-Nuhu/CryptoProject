using CryptoProject.Entities.Enums;

namespace CryptoProject.Models.Requests
{
    public class GetBalanceRequest
    {
        public Guid UserId { get; set; }
        public WalletType WalletType { get; set; }
    }
}
