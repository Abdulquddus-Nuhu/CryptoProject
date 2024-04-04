using CryptoProject.Entities.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoProject.Entities
{
    public class Wallet : BaseEntity
    {
        public decimal Balance { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }
    }
}
