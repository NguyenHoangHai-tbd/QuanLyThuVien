using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using QLyThuVien.Application.Features.BookCopies.Commands.Create;
using QLyThuVien.Application.Features.BookCopies.Commands.Delete;
using QLyThuVien.Application.Features.BookCopies.Commands.Update;
using QLyThuVien.Application.Features.BookCopies.Common;
using QLyThuVien.Application.Features.BookCopies.Queries;
using QLyThuVien.Application.Features.Books.Commands.Create;
using QLyThuVien.Application.Features.Books.Commands.Delete;
using QLyThuVien.Application.Features.Books.Commands.Update;
using QLyThuVien.Application.Features.Books.Common;
using QLyThuVien.Application.Features.Books.Queries;

namespace QLyThuVien.Api.Controllers;

[ApiController]
[Route("api/catalog")]
[Authorize]
public sealed class CatalogController : ControllerBase
{
    private readonly ISender _sender;

    public CatalogController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("books")]
    public Task<IReadOnlyCollection<BookListItemDto>> SearchBooks([FromQuery] string? search, [FromQuery] Guid? branchId, CancellationToken cancellationToken)
        => _sender.Send(new SearchBooksQuery(search, branchId), cancellationToken);

    [HttpGet("books/{id:guid}")]
    public Task<BookDto> GetBook(Guid id, CancellationToken cancellationToken)
        => _sender.Send(new GetBookQuery(id), cancellationToken);

    [HttpPost("books")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Librarian,InventoryStaff")]
    public Task<BookDto> CreateBook(CreateBookRequest request, CancellationToken cancellationToken)
        => _sender.Send(new CreateBookCommand(request), cancellationToken);

    [HttpPut("books/{id:guid}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Librarian,InventoryStaff")]
    public Task<BookDto> UpdateBook(Guid id, UpdateBookRequest request, CancellationToken cancellationToken)
        => _sender.Send(new UpdateBookCommand(id, request), cancellationToken);

    [HttpDelete("books/{id:guid}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Librarian,InventoryStaff")]
    public async Task<IActionResult> DeleteBook(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteBookCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpGet("copies")]
    public Task<IReadOnlyCollection<BookCopyDto>> GetCopies([FromQuery] Guid? bookId, [FromQuery] Guid? branchId, CancellationToken cancellationToken)
        => _sender.Send(new GetCopiesQuery(bookId, branchId), cancellationToken);

    [HttpPost("copies")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Librarian,InventoryStaff")]
    public Task<BookCopyDto> CreateCopy(CreateCopyRequest request, CancellationToken cancellationToken)
        => _sender.Send(new CreateCopyCommand(request), cancellationToken);

    [HttpPut("copies/{id:guid}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Librarian,InventoryStaff")]
    public Task<BookCopyDto> UpdateCopy(Guid id, UpdateCopyRequest request, CancellationToken cancellationToken)
        => _sender.Send(new UpdateCopyCommand(id, request), cancellationToken);

    [HttpDelete("copies/{id:guid}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Librarian,InventoryStaff")]
    public async Task<IActionResult> DeleteCopy(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteCopyCommand(id), cancellationToken);
        return NoContent();
    }
}
