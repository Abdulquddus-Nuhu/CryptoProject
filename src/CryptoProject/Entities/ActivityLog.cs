using CryptoProject.Entities.Enums;
using CryptoProject.Entities.Identity;
using System.ComponentModel.DataAnnotations;

namespace CryptoProject.Entities
{
    public class ActivityLog : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public User? User { get; set; }

        [StringLength(50, MinimumLength = 1)]
        public string? UserEmail { get; set; }
        public ActivityType ActivityType { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Details { get; set; } = string.Empty;
        public string? Data { get; set; }
    }
}
