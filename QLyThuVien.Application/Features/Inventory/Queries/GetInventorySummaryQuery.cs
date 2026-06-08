using MediatR;
using QLyThuVien.Application.Features.Inventory.Common;

namespace QLyThuVien.Application.Features.Inventory.Queries;

public sealed record GetInventorySummaryQuery(Guid? BranchId) : IRequest<InventorySummaryDto>;
