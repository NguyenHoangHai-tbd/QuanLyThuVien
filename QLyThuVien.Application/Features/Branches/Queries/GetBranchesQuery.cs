using MediatR;
using QLyThuVien.Application.Features.Branches.Common;

namespace QLyThuVien.Application.Features.Branches.Queries;

public sealed record GetBranchesQuery : IRequest<IReadOnlyCollection<BranchDto>>;

