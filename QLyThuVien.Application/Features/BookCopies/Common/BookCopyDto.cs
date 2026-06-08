using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Features.BookCopies.Common;

public sealed record BookCopyDto(Guid Id, Guid BookId, Guid BranchId, string BranchName, string Barcode, string QrCode, BookCopyStatus Status, string Location);

