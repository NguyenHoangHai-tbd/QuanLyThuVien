using MediatR;
using QLyThuVien.Application.Features.Books.Common;

namespace QLyThuVien.Application.Features.Books.Queries;

public sealed record GetBookQuery(Guid Id) : IRequest<BookDto>;

