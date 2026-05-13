using QLyThuVien.Application.Abstractions;
using QLyThuVien.Application.Dtos;

namespace QLyThuVien.Application.Services;

public sealed class AuditService : ApplicationServiceBase
{
    public AuditService(ILibraryRepository repository, ICurrentUserContext currentUser, IClock clock)
        : base(repository, currentUser, clock)
    {
    }

    public Task<IReadOnlyCollection<AuditLogDto>> GetAuditLogsAsync(Guid? branchId, CancellationToken cancellationToken = default)
    {
        var logs = TenantScope(Repository.AuditLogs)
            .Where(x => !x.BranchId.HasValue || CurrentUser.CanAccessBranch(x.BranchId.Value));

        if (branchId.HasValue)
        {
            EnsureBranchAccess(branchId.Value);
            logs = logs.Where(x => x.BranchId == branchId.Value);
        }

        var result = logs
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .Select(x => new AuditLogDto(x.Id, x.BranchId, x.ActorName, x.Action, x.EntityName, x.EntityId, x.Summary, x.CreatedAt))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<AuditLogDto>>(result);
    }
}
