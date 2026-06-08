namespace QLyThuVien.Application.Features.Members.Common;

public sealed record MemberRequest(Guid BranchId, string Code, string FullName, string Email, string Phone);
