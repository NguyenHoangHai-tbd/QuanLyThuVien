using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Features.BookCopies.Common;

public sealed record UpdateCopyRequest(Guid BranchId, string Barcode, string Location, BookCopyStatus Status);

