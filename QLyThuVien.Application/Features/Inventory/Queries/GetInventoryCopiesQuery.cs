using MediatR;
using QLyThuVien.Application.Features.Inventory.Common;
using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Features.Inventory.Queries;

public sealed record GetInventoryCopiesQuery(Guid? BranchId, BookCopyStatus? Status) : IRequest<IReadOnlyCollection<InventoryItemDto>>;
