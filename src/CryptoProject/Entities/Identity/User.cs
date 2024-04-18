using CryptoProject.Entities.Enums;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CryptoProject.Entities.Identity
{
    public class User : IdentityUser<Guid>
    {
        [StringLength(50)]
        public string FirstName { get; set; }

        [StringLength(50)]
        public string LastName { get; set; }

        [StringLength(50)]
        public string? MiddleName { get; set; }

        public string FullName { get => $"{FirstName} {LastName}"; }

        public RoleType Role { get; set; } 
        public AccountType AccountType { get; set; }

        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }


        [StringLength(50)]
        public string? Pin { get; set; }

        public string? PinHash { get; set; }
        public bool IsActive { get; set; } = false;
        public Guid? WalletId { get; set; }
        public Wallet Wallet { get; set; }

        public Guid? LedgerAccountId { get; set; }
        public LedgerAccount? LedgerAccount { get; set; }

        public Guid? USDAccountId { get; set; }
        public USDAccount? USDAccount { get; set; }

        public string? Country { get; set; }
        public string? AccountNumber { get; set; }
        public string? Password { get; set; }



        public bool IsDeleted { get; set; }
        public string? DeletedBy { get; protected set; } = string.Empty;
        public virtual DateTime? Deleted { get; protected set; }
        public virtual DateTime Created { get; set; }
        public virtual DateTime? Modified { get; protected set; }
        public virtual string? LastModifiedBy { get; protected set; }

        public User(DateTime created, bool isDeleted)
        {
            Created = created;
            IsDeleted = isDeleted;
            Id = Guid.NewGuid();
        }
        public User() : this(DateTime.UtcNow, false) { }
    }
}
