using MediatR;
using QLyThuVien.Application.Common;
using QLyThuVien.Application.Features.AuditLogs.Common;
using QLyThuVien.Application.Features.AuditLogs.Queries;
using QLyThuVien.Application.Interfaces;

namespace QLyThuVien.Application.Features.AuditLogs.Handlers;

public sealed class AuditLogHandler : ApplicationRequestHandlerBase, IRequestHandler<GetAuditLogsQuery, IReadOnlyCollection<AuditLogDto>>
{
    public AuditLogHandler(ILibraryRepository repository, ICurrentUserContext currentUser, IClock clock)
        : base(repository, currentUser, clock)
    {
    }

    public Task<IReadOnlyCollection<AuditLogDto>> Handle(GetAuditLogsQuery query, CancellationToken cancellationToken)
    {
        var logs = TenantScope(Repository.AuditLogs)
            .Where(x => !x.BranchId.HasValue || CurrentUser.CanAccessBranch(x.BranchId.Value));

        if (query.BranchId.HasValue)
        {
            EnsureBranchAccess(query.BranchId.Value);
            logs = logs.Where(x => x.BranchId == query.BranchId.Value);
        }

        var result = logs
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .Select(x => new AuditLogDto(x.Id, x.BranchId, x.ActorName, x.Action, x.EntityName, x.EntityId, x.Summary, x.CreatedAt))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<AuditLogDto>>(result);
    }
}
