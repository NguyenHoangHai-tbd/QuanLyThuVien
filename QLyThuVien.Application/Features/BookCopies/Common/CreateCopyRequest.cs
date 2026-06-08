namespace QLyThuVien.Application.Features.BookCopies.Common;

public sealed record CreateCopyRequest(Guid BookId, Guid BranchId, string Barcode, string Location);

