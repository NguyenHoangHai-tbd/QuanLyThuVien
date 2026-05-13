using QLyThuVien.Application.Abstractions;
using QLyThuVien.Application.Common;
using QLyThuVien.Domain.Common;
using QLyThuVien.Domain.Entities;

namespace QLyThuVien.Application.Services;

public abstract class ApplicationServiceBase
{
    protected ApplicationServiceBase(ILibraryRepository repository, ICurrentUserContext currentUser, IClock clock)
    {
        Repository = repository;
        CurrentUser = currentUser;
        Clock = clock;
    }

    protected ILibraryRepository Repository { get; }

    protected ICurrentUserContext CurrentUser { get; }

    protected IClock Clock { get; }

    protected Guid TenantId => CurrentUser.TenantId;

    protected void EnsureAuthenticated()
    {
        if (!CurrentUser.IsAuthenticated)
        {
            throw AppException.Unauthorized();
        }
    }

    protected void EnsureBranchAccess(Guid branchId)
    {
        EnsureAuthenticated();

        if (!CurrentUser.CanAccessBranch(branchId))
        {
            throw AppException.Forbidden("User is not allowed to access this branch.");
        }
    }

    protected IEnumerable<T> TenantScope<T>(IEnumerable<T> source)
        where T : TenantEntity
    {
        EnsureAuthenticated();

        return source.Where(x => x.TenantId == TenantId && !x.IsDeleted);
    }

    protected IEnumerable<T> BranchScope<T>(IEnumerable<T> source)
        where T : BranchEntity
    {
        return TenantScope(source).Where(x => CurrentUser.CanAccessBranch(x.BranchId));
    }

    protected Branch GetBranch(Guid branchId)
    {
        var branch = TenantScope(Repository.Branches).FirstOrDefault(x => x.Id == branchId);
        if (branch is null)
        {
            throw AppException.NotFound("Branch not found.");
        }

        EnsureBranchAccess(branch.Id);
        return branch;
    }

    protected void AddAudit(string action, string entityName, Guid? entityId, string summary, Guid? branchId = null)
    {
        Repository.AddAuditLog(new AuditLog
        {
            TenantId = TenantId,
            BranchId = branchId,
            ActorUserId = CurrentUser.UserId,
            ActorName = CurrentUser.UserName,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Summary = summary,
            CreatedBy = CurrentUser.Email,
            CreatedAt = Clock.UtcNow
        });
    }

    protected static string Clean(string? value) => (value ?? string.Empty).Trim();

    protected static bool HasText(string? value, string search)
        => !string.IsNullOrWhiteSpace(value) && value.Contains(search, StringComparison.OrdinalIgnoreCase);
}
