using MediatR;

namespace QLyThuVien.Application.Features.Books.Commands.Delete;

public sealed record DeleteBookCommand(Guid Id) : IRequest;

