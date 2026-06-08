using MediatR;
using QLyThuVien.Application.Features.Fines.Common;

namespace QLyThuVien.Application.Features.Fines.Queries;

public sealed record GetFinesQuery(bool UnpaidOnly, Guid? BranchId) : IRequest<IReadOnlyCollection<FineDto>>;
