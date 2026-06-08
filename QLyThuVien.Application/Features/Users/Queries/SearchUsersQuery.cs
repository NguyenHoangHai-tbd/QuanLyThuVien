using MediatR;
using QLyThuVien.Application.Features.Users.Common;

namespace QLyThuVien.Application.Features.Users.Queries;

public sealed record SearchUsersQuery(string? Search, Guid? BranchId) : IRequest<IReadOnlyCollection<UserAccountDto>>;
