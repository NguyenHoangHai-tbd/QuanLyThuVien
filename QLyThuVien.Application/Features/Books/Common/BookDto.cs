using QLyThuVien.Application.Features.BookCopies.Common;

namespace QLyThuVien.Application.Features.Books.Common;

public sealed record BookDto(
    Guid Id,
    string Title,
    string Isbn,
    string Description,
    int? PublishedYear,
    string Language,
    IReadOnlyCollection<string> Authors,
    IReadOnlyCollection<string> Categories,
    string Publisher,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<BookCopyDto> Copies);

