using MediatR;

namespace QLyThuVien.Application.Features.BookCopies.Commands.Delete;

public sealed record DeleteCopyCommand(Guid Id) : IRequest;

