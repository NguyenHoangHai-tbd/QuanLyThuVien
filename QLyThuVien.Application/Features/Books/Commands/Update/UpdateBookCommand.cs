using MediatR;
using QLyThuVien.Application.Features.Books.Common;

namespace QLyThuVien.Application.Features.Books.Commands.Update;

public sealed record UpdateBookCommand(Guid Id, UpdateBookRequest Request) : IRequest<BookDto>;

