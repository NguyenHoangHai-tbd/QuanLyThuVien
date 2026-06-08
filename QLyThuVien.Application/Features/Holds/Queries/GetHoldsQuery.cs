using MediatR;
using QLyThuVien.Application.Features.Holds.Common;

namespace QLyThuVien.Application.Features.Holds.Queries;

public sealed record GetHoldsQuery(Guid? BranchId) : IRequest<IReadOnlyCollection<HoldDto>>;

