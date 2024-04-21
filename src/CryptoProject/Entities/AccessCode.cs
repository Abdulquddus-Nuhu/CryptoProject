using System.ComponentModel.DataAnnotations;

namespace CryptoProject.Entities
{
    public class AccessCode : BaseEntity
    {
        [StringLength(50, MinimumLength = 1)]
        public string Code { get; set; }
    }
}
