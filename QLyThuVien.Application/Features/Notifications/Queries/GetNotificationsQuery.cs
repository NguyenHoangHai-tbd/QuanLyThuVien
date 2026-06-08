using MediatR;
using QLyThuVien.Application.Features.Notifications.Common;

namespace QLyThuVien.Application.Features.Notifications.Queries;

public sealed record GetNotificationsQuery(Guid? BranchId) : IRequest<IReadOnlyCollection<NotificationDto>>;

