using MediatR;
using QLyThuVien.Application.Features.Loans.Common;

namespace QLyThuVien.Application.Features.Loans.Commands.Create;

public sealed record CreateLoanCommand(LoanRequest Request) : IRequest<LoanDto>;

