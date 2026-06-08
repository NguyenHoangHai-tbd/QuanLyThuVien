namespace QLyThuVien.Application.Features.Books.Common;

public sealed record CreateBookRequest(
    string Title,
    string Isbn,
    string Description,
    int? PublishedYear,
    string Language,
    string Publisher,
    IReadOnlyCollection<string> Authors,
    IReadOnlyCollection<string> Categories,
    IReadOnlyCollection<string> Tags);

