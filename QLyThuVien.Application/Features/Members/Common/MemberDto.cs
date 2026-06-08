using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Features.Members.Common;

public sealed record MemberDto(Guid Id, Guid BranchId, string BranchName, string Code, string FullName, string Email, string Phone, MemberStatus Status, DateTimeOffset JoinedAt);
