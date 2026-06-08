using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using QLyThuVien.Application.Features.Fines.Commands.Pay;
using QLyThuVien.Application.Features.Fines.Common;
using QLyThuVien.Application.Features.Fines.Queries;

namespace QLyThuVien.Api.Controllers;

[ApiController]
[Route("api/circulation/fines")]
[Authorize(Roles = "SuperAdmin,TenantAdmin,Librarian")]
public sealed class FinesController : ControllerBase
{
    private readonly ISender _sender;

    public FinesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public Task<IReadOnlyCollection<FineDto>> GetFines(
        [FromQuery] bool unpaidOnly = true,
        [FromQuery] Guid? branchId = null,
        CancellationToken cancellationToken = default)
        => _sender.Send(new GetFinesQuery(unpaidOnly, branchId), cancellationToken);

    [HttpPost("{loanId:guid}/pay")]
    public Task<FineDto> PayFine(Guid loanId, PayFineRequest request, CancellationToken cancellationToken)
        => _sender.Send(new PayFineCommand(loanId, request), cancellationToken);
}
