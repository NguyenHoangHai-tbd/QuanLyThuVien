namespace QLyThuVien.Application.Features.Branches.Common;

public sealed record BranchUpdateRequest(string Code, string Name, string Address, bool IsActive);

