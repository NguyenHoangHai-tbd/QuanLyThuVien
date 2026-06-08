using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Features.Inventory.Common;

public sealed record StocktakeCopyRequest(Guid BranchId, string Barcode, BookCopyStatus Status, string Location, string? Note);
