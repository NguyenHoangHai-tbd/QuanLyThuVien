using MediatR;
using QLyThuVien.Application.Features.Books.Common;

namespace QLyThuVien.Application.Features.Books.Queries;

public sealed record SearchBooksQuery(string? Search, Guid? BranchId) : IRequest<IReadOnlyCollection<BookListItemDto>>;

