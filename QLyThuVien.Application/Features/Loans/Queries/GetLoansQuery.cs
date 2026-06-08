using MediatR;
using QLyThuVien.Application.Features.Loans.Common;

namespace QLyThuVien.Application.Features.Loans.Queries;

public sealed record GetLoansQuery(bool ActiveOnly, Guid? BranchId) : IRequest<IReadOnlyCollection<LoanDto>>;

