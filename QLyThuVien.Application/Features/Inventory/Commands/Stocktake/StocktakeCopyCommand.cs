using MediatR;
using QLyThuVien.Application.Features.Inventory.Common;

namespace QLyThuVien.Application.Features.Inventory.Commands.Stocktake;

public sealed record StocktakeCopyCommand(StocktakeCopyRequest Request) : IRequest<InventoryItemDto>;
