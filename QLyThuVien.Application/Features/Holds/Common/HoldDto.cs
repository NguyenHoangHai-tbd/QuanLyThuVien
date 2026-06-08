using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Features.Holds.Common;

public sealed record HoldDto(Guid Id, Guid BookId, string BookTitle, Guid MemberId, string MemberName, Guid BranchId, HoldStatus Status, DateTimeOffset RequestedAt, DateTimeOffset? ExpiresAt);

