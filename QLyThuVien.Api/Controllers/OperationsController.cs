using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using QLyThuVien.Application.Features.AuditLogs.Common;
using QLyThuVien.Application.Features.AuditLogs.Queries;
using QLyThuVien.Application.Features.Dashboard.Common;
using QLyThuVien.Application.Features.Dashboard.Queries;
using QLyThuVien.Application.Features.Notifications.Common;
using QLyThuVien.Application.Features.Notifications.Queries;

namespace QLyThuVien.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public sealed class OperationsController : ControllerBase
{
    private readonly ISender _sender;

    public OperationsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("dashboard/summary")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Librarian,InventoryStaff")]
    public Task<DashboardSummaryDto> GetDashboard(CancellationToken cancellationToken)
        => _sender.Send(new GetDashboardSummaryQuery(), cancellationToken);

    [HttpGet("notifications")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Librarian,InventoryStaff")]
    public Task<IReadOnlyCollection<NotificationDto>> GetNotifications([FromQuery] Guid? branchId, CancellationToken cancellationToken)
        => _sender.Send(new GetNotificationsQuery(branchId), cancellationToken);

    [HttpGet("audit-logs")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    public Task<IReadOnlyCollection<AuditLogDto>> GetAuditLogs([FromQuery] Guid? branchId, CancellationToken cancellationToken)
        => _sender.Send(new GetAuditLogsQuery(branchId), cancellationToken);
}
