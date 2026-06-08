namespace QLyThuVien.Application.Features.Holds.Common;

public sealed record HoldRequestPayload(Guid BookId, Guid MemberId, Guid BranchId);

