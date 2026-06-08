using MediatR;
using QLyThuVien.Application.Common;
using QLyThuVien.Application.Features.Notifications.Common;
using QLyThuVien.Application.Features.Notifications.Queries;
using QLyThuVien.Application.Interfaces;

namespace QLyThuVien.Application.Features.Notifications.Handlers;

public sealed class NotificationHandler : ApplicationRequestHandlerBase, IRequestHandler<GetNotificationsQuery, IReadOnlyCollection<NotificationDto>>
{
    public NotificationHandler(ILibraryRepository repository, ICurrentUserContext currentUser, IClock clock)
        : base(repository, currentUser, clock)
    {
    }

    public Task<IReadOnlyCollection<NotificationDto>> Handle(GetNotificationsQuery query, CancellationToken cancellationToken)
    {
        var notifications = TenantScope(Repository.Notifications)
            .Where(x => !x.BranchId.HasValue || CurrentUser.CanAccessBranch(x.BranchId.Value));

        if (query.BranchId.HasValue)
        {
            EnsureBranchAccess(query.BranchId.Value);
            notifications = notifications.Where(x => x.BranchId == query.BranchId.Value);
        }

        var result = notifications
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .Select(x => new NotificationDto(x.Id, x.BranchId, x.Channel, x.MessageKey, x.Variables, x.Status, x.CreatedAt))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<NotificationDto>>(result);
    }
}
