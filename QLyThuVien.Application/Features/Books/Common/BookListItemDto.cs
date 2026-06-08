namespace QLyThuVien.Application.Features.Books.Common;

public sealed record BookListItemDto(
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
    int TotalCopies,
    int AvailableCopies);

