using MediatR;
using QLyThuVien.Application.Features.Books.Common;

namespace QLyThuVien.Application.Features.Books.Commands.Create;

public sealed record CreateBookCommand(CreateBookRequest Request) : IRequest<BookDto>;

