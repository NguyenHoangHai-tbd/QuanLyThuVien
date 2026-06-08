namespace QLyThuVien.Application.Features.AuditLogs.Common;

public sealed record AuditLogDto(Guid Id, Guid? BranchId, string ActorName, string Action, string EntityName, Guid? EntityId, string Summary, DateTimeOffset CreatedAt);

