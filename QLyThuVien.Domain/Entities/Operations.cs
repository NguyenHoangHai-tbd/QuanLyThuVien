using QLyThuVien.Domain.Common;
using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Domain.Entities;

public sealed class NotificationMessage : TenantEntity
{
    public Guid? BranchId { get; set; }

    public Guid? UserId { get; set; }

    public Guid? MemberId { get; set; }

    public string Channel { get; set; } = "in-app";

    public string MessageKey { get; set; } = string.Empty;

    public Dictionary<string, string> Variables { get; set; } = [];

    public NotificationStatus Status { get; set; } = NotificationStatus.Queued;
}

public sealed class AuditLog : TenantEntity
{
    public Guid? BranchId { get; set; }

    public Guid? ActorUserId { get; set; }

    public string ActorName { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;

    public Guid? EntityId { get; set; }

    public string Summary { get; set; } = string.Empty;
}

public sealed class AiUsageLog : TenantEntity
{
    public Guid? BranchId { get; set; }

    public Guid UserId { get; set; }

    public string Feature { get; set; } = string.Empty;

    public string Prompt { get; set; } = string.Empty;

    public int ResultCount { get; set; }

    public bool UsedFallback { get; set; }
}
