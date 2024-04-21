using System.ComponentModel.DataAnnotations;

namespace CryptoProject.Entities
{
    public class CryptoWallet : BaseEntity
    {
        [StringLength(50, MinimumLength = 1)]
        public string? Address { get; set; }
    }
}
