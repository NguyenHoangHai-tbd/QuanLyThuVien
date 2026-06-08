using MediatR;
using QLyThuVien.Application.Features.BookCopies.Common;

namespace QLyThuVien.Application.Features.BookCopies.Commands.Create;

public sealed record CreateCopyCommand(CreateCopyRequest Request) : IRequest<BookCopyDto>;

