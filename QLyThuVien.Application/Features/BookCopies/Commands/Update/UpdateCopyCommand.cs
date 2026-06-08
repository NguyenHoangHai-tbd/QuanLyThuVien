using MediatR;
using QLyThuVien.Application.Features.BookCopies.Common;

namespace QLyThuVien.Application.Features.BookCopies.Commands.Update;

public sealed record UpdateCopyCommand(Guid Id, UpdateCopyRequest Request) : IRequest<BookCopyDto>;

