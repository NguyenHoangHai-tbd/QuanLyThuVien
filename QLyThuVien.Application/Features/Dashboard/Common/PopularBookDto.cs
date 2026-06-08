namespace QLyThuVien.Application.Features.Dashboard.Common;

public sealed record PopularBookDto(Guid BookId, string Title, int LoanCount);

