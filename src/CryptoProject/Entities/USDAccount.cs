using CryptoProject.Entities.Identity;

namespace CryptoProject.Entities
{
    public class USDAccount : BaseEntity
    {
        public decimal Balance { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }
    }
}
