using MediatR;
using QLyThuVien.Application.Features.Dashboard.Common;

namespace QLyThuVien.Application.Features.Dashboard.Queries;

public sealed record GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>;

