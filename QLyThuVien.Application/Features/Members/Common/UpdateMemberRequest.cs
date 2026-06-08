using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Features.Members.Common;

public sealed record UpdateMemberRequest(Guid BranchId, string Code, string FullName, string Email, string Phone, MemberStatus Status);
