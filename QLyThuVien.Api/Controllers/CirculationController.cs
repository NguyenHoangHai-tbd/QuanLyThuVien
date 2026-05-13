using Microsoft.AspNetCore.Mvc;
using QLyThuVien.Application.Dtos;
using QLyThuVien.Application.Services;

namespace QLyThuVien.Api.Controllers;

[ApiController]
[Route("api/circulation")]
public sealed class CirculationController : ControllerBase
{
    private readonly CirculationService _circulationService;

    public CirculationController(CirculationService circulationService)
    {
        _circulationService = circulationService;
    }

    [HttpGet("loans")]
    public Task<IReadOnlyCollection<LoanDto>> GetLoans([FromQuery] bool activeOnly = false, [FromQuery] Guid? branchId = null, CancellationToken cancellationToken = default)
        => _circulationService.GetLoansAsync(activeOnly, branchId, cancellationToken);

    [HttpPost("loans")]
    public Task<LoanDto> CreateLoan(LoanRequest request, CancellationToken cancellationToken)
        => _circulationService.CreateLoanAsync(request, cancellationToken);

    [HttpPost("returns")]
    public Task<LoanDto> Return(ReturnRequest request, CancellationToken cancellationToken)
        => _circulationService.ReturnAsync(request, cancellationToken);

    [HttpPost("renewals")]
    public Task<LoanDto> Renew(RenewRequest request, CancellationToken cancellationToken)
        => _circulationService.RenewAsync(request, cancellationToken);

    [HttpGet("holds")]
    public Task<IReadOnlyCollection<HoldDto>> GetHolds([FromQuery] Guid? branchId, CancellationToken cancellationToken)
        => _circulationService.GetHoldsAsync(branchId, cancellationToken);

    [HttpPost("holds")]
    public Task<HoldDto> CreateHold(HoldRequestPayload request, CancellationToken cancellationToken)
        => _circulationService.CreateHoldAsync(request, cancellationToken);
}
