using MediatR;
using QLyThuVien.Application.Features.BookCopies.Common;

namespace QLyThuVien.Application.Features.BookCopies.Queries;

public sealed record GetCopiesQuery(Guid? BookId, Guid? BranchId) : IRequest<IReadOnlyCollection<BookCopyDto>>;

