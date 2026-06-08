using MediatR;
using QLyThuVien.Application.Features.AuditLogs.Common;

namespace QLyThuVien.Application.Features.AuditLogs.Queries;

public sealed record GetAuditLogsQuery(Guid? BranchId) : IRequest<IReadOnlyCollection<AuditLogDto>>;

