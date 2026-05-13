using QLyThuVien.Application.Abstractions;
using QLyThuVien.Application.Dtos;

namespace QLyThuVien.Application.Services;

public sealed class NotificationService : ApplicationServiceBase
{
    public NotificationService(ILibraryRepository repository, ICurrentUserContext currentUser, IClock clock)
        : base(repository, currentUser, clock)
    {
    }

    public Task<IReadOnlyCollection<NotificationDto>> GetNotificationsAsync(Guid? branchId, CancellationToken cancellationToken = default)
    {
        var notifications = TenantScope(Repository.Notifications)
            .Where(x => !x.BranchId.HasValue || CurrentUser.CanAccessBranch(x.BranchId.Value));

        if (branchId.HasValue)
        {
            EnsureBranchAccess(branchId.Value);
            notifications = notifications.Where(x => x.BranchId == branchId.Value);
        }

        var result = notifications
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .Select(x => new NotificationDto(x.Id, x.BranchId, x.Channel, x.MessageKey, x.Variables, x.Status, x.CreatedAt))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<NotificationDto>>(result);
    }
}
