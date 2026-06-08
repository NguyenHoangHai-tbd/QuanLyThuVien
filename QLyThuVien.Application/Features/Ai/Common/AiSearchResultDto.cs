namespace QLyThuVien.Application.Features.Ai.Common;

public sealed record AiSearchResultDto(Guid BookId, string Title, string Isbn, int AvailableCopies, decimal Score, string Explanation);
