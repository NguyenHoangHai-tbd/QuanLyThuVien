using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Features.Inventory.Common;

public sealed record InventoryItemDto(
    Guid CopyId,
    Guid BookId,
    string BookTitle,
    Guid BranchId,
    string BranchName,
    string Barcode,
    string QrCode,
    BookCopyStatus Status,
    string Location);
