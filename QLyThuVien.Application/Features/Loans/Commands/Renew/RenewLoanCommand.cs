using MediatR;
using QLyThuVien.Application.Features.Loans.Common;

namespace QLyThuVien.Application.Features.Loans.Commands.Renew;

public sealed record RenewLoanCommand(RenewRequest Request) : IRequest<LoanDto>;

