namespace QLyThuVien.Application.Features.Branches.Common;

public sealed record BranchDto(Guid Id, string Code, string Name, string Address, bool IsActive);

