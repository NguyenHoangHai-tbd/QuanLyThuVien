using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using QLyThuVien.Application.Features.Holds.Commands.Cancel;
using QLyThuVien.Application.Features.Holds.Commands.Create;
using QLyThuVien.Application.Features.Holds.Common;
using QLyThuVien.Application.Features.Holds.Queries;
using QLyThuVien.Application.Features.Loans.Commands.Create;
using QLyThuVien.Application.Features.Loans.Commands.Renew;
using QLyThuVien.Application.Features.Loans.Commands.Return;
using QLyThuVien.Application.Features.Loans.Common;
using QLyThuVien.Application.Features.Loans.Queries;

namespace QLyThuVien.Api.Controllers;

[ApiController]
[Route("api/circulation")]
[Authorize(Roles = "SuperAdmin,TenantAdmin,Librarian")]
public sealed class CirculationController : ControllerBase
{
    private readonly ISender _sender;

    public CirculationController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("loans")]
    public Task<IReadOnlyCollection<LoanDto>> GetLoans([FromQuery] bool activeOnly = false, [FromQuery] Guid? branchId = null, CancellationToken cancellationToken = default)
        => _sender.Send(new GetLoansQuery(activeOnly, branchId), cancellationToken);

    [HttpPost("loans")]
    public Task<LoanDto> CreateLoan(LoanRequest request, CancellationToken cancellationToken)
        => _sender.Send(new CreateLoanCommand(request), cancellationToken);

    [HttpPost("returns")]
    public Task<LoanDto> Return(ReturnRequest request, CancellationToken cancellationToken)
        => _sender.Send(new ReturnLoanCommand(request), cancellationToken);

    [HttpPost("renewals")]
    public Task<LoanDto> Renew(RenewRequest request, CancellationToken cancellationToken)
        => _sender.Send(new RenewLoanCommand(request), cancellationToken);

    [HttpGet("holds")]
    public Task<IReadOnlyCollection<HoldDto>> GetHolds([FromQuery] Guid? branchId, CancellationToken cancellationToken)
        => _sender.Send(new GetHoldsQuery(branchId), cancellationToken);

    [HttpPost("holds")]
    public Task<HoldDto> CreateHold(HoldRequestPayload request, CancellationToken cancellationToken)
        => _sender.Send(new CreateHoldCommand(request), cancellationToken);

    [HttpPost("holds/{id:guid}/cancel")]
    public Task<HoldDto> CancelHold(Guid id, CancellationToken cancellationToken)
        => _sender.Send(new CancelHoldCommand(id), cancellationToken);
}
