using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using QLyThuVien.Application.Features.Inventory.Commands.Stocktake;
using QLyThuVien.Application.Features.Inventory.Common;
using QLyThuVien.Application.Features.Inventory.Queries;
using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Api.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize(Roles = "SuperAdmin,TenantAdmin,Librarian,InventoryStaff")]
public sealed class InventoryController : ControllerBase
{
    private readonly ISender _sender;

    public InventoryController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("summary")]
    public Task<InventorySummaryDto> GetSummary([FromQuery] Guid? branchId, CancellationToken cancellationToken)
        => _sender.Send(new GetInventorySummaryQuery(branchId), cancellationToken);

    [HttpGet("copies")]
    public Task<IReadOnlyCollection<InventoryItemDto>> GetCopies(
        [FromQuery] Guid? branchId,
        [FromQuery] BookCopyStatus? status,
        CancellationToken cancellationToken)
        => _sender.Send(new GetInventoryCopiesQuery(branchId, status), cancellationToken);

    [HttpPost("copies/stocktake")]
    public Task<InventoryItemDto> Stocktake(StocktakeCopyRequest request, CancellationToken cancellationToken)
        => _sender.Send(new StocktakeCopyCommand(request), cancellationToken);
}
