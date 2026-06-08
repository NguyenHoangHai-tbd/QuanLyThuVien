namespace QLyThuVien.Application.Features.Inventory.Common;

public sealed record InventorySummaryDto(
    Guid? BranchId,
    string BranchName,
    int TotalCopies,
    int AvailableCopies,
    int OnLoanCopies,
    int ReservedCopies,
    int DamagedCopies,
    int LostCopies,
    int LiquidatedCopies);
