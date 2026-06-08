using MediatR;
using QLyThuVien.Application.Features.Fines.Common;

namespace QLyThuVien.Application.Features.Fines.Commands.Pay;

public sealed record PayFineCommand(Guid LoanId, PayFineRequest Request) : IRequest<FineDto>;
