using MediatR;
using QLyThuVien.Application.Features.Members.Common;

namespace QLyThuVien.Application.Features.Members.Queries;

public sealed record SearchMembersQuery(string? Search, Guid? BranchId) : IRequest<IReadOnlyCollection<MemberDto>>;
