namespace CryptoProject.Entities
{
    public class BaseEntity : IBaseEntity
    {
        public Guid Id { get; set; }
        public bool IsDeleted { get; set; }
        public string? DeletedBy { get; protected set; } = string.Empty;
        public virtual DateTime? Deleted { get; protected set; }
        public string? CreatedBy { get; set; } = string.Empty;
        public virtual DateTime Created { get; set; }
        public virtual DateTime? Modified { get; protected set; }
        public virtual string? LastModifiedBy { get; protected set; }
        protected BaseEntity()
        {
            Id = Guid.NewGuid();
            IsDeleted = false;
            Created = DateTime.UtcNow;
        }
    }

}
