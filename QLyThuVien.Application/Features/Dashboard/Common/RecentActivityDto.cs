namespace QLyThuVien.Application.Features.Dashboard.Common;

public sealed record RecentActivityDto(string Action, string Summary, DateTimeOffset CreatedAt);

