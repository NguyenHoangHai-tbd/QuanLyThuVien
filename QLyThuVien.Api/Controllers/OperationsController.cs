using Microsoft.AspNetCore.Mvc;
using QLyThuVien.Application.Dtos;
using QLyThuVien.Application.Services;

namespace QLyThuVien.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class OperationsController : ControllerBase
{
    private readonly AiService _aiService;
    private readonly AuditService _auditService;
    private readonly DashboardService _dashboardService;
    private readonly NotificationService _notificationService;

    public OperationsController(
        DashboardService dashboardService,
        NotificationService notificationService,
        AuditService auditService,
        AiService aiService)
    {
        _dashboardService = dashboardService;
        _notificationService = notificationService;
        _auditService = auditService;
        _aiService = aiService;
    }

    [HttpGet("dashboard/summary")]
    public Task<DashboardSummaryDto> GetDashboard(CancellationToken cancellationToken)
        => _dashboardService.GetSummaryAsync(cancellationToken);

    [HttpGet("notifications")]
    public Task<IReadOnlyCollection<NotificationDto>> GetNotifications([FromQuery] Guid? branchId, CancellationToken cancellationToken)
        => _notificationService.GetNotificationsAsync(branchId, cancellationToken);

    [HttpGet("audit-logs")]
    public Task<IReadOnlyCollection<AuditLogDto>> GetAuditLogs([FromQuery] Guid? branchId, CancellationToken cancellationToken)
        => _auditService.GetAuditLogsAsync(branchId, cancellationToken);

    [HttpPost("ai/search")]
    public Task<AiSearchResponse> AiSearch(AiSearchRequest request, CancellationToken cancellationToken)
        => _aiService.SemanticSearchAsync(request, cancellationToken);

    [HttpPost("ai/chat")]
    public Task<AiChatResponse> AiChat(AiChatRequest request, CancellationToken cancellationToken)
        => _aiService.ChatAsync(request, cancellationToken);
}
