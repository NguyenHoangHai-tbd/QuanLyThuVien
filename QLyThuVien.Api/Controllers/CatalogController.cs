using Microsoft.AspNetCore.Mvc;
using QLyThuVien.Application.Dtos;
using QLyThuVien.Application.Services;

namespace QLyThuVien.Api.Controllers;

[ApiController]
[Route("api/catalog")]
public sealed class CatalogController : ControllerBase
{
    private readonly CatalogService _catalogService;

    public CatalogController(CatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet("books")]
    public Task<IReadOnlyCollection<BookListItemDto>> SearchBooks([FromQuery] string? search, [FromQuery] Guid? branchId, CancellationToken cancellationToken)
        => _catalogService.SearchBooksAsync(search, branchId, cancellationToken);

    [HttpGet("books/{id:guid}")]
    public Task<BookDto> GetBook(Guid id, CancellationToken cancellationToken)
        => _catalogService.GetBookAsync(id, cancellationToken);

    [HttpPost("books")]
    public Task<BookDto> CreateBook(CreateBookRequest request, CancellationToken cancellationToken)
        => _catalogService.CreateBookAsync(request, cancellationToken);

    [HttpGet("copies")]
    public Task<IReadOnlyCollection<BookCopyDto>> GetCopies([FromQuery] Guid? bookId, [FromQuery] Guid? branchId, CancellationToken cancellationToken)
        => _catalogService.GetCopiesAsync(bookId, branchId, cancellationToken);

    [HttpPost("copies")]
    public Task<BookCopyDto> CreateCopy(CreateCopyRequest request, CancellationToken cancellationToken)
        => _catalogService.CreateCopyAsync(request, cancellationToken);
}
