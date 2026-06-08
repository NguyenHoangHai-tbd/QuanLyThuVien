using MediatR;
using QLyThuVien.Application.Features.Loans.Common;

namespace QLyThuVien.Application.Features.Loans.Commands.Return;

public sealed record ReturnLoanCommand(ReturnRequest Request) : IRequest<LoanDto>;

