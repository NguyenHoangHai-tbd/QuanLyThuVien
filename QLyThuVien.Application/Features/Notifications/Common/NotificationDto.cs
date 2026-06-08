using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Features.Notifications.Common;

public sealed record NotificationDto(Guid Id, Guid? BranchId, string Channel, string MessageKey, IReadOnlyDictionary<string, string> Variables, NotificationStatus Status, DateTimeOffset CreatedAt);

