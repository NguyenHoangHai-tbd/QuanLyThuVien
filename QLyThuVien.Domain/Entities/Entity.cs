namespace QLyThuVien.Domain.Entities;

public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}

public abstract class AuditableEntity : Entity
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string CreatedBy { get; set; } = "system";

    public DateTimeOffset? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
}

public abstract class TenantEntity : AuditableEntity
{
    public Guid TenantId { get; set; }
}

public abstract class BranchEntity : TenantEntity
{
    public Guid BranchId { get; set; }
}
