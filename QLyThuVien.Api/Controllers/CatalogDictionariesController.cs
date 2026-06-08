using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using QLyThuVien.Application.Features.Authors.Commands.Create;
using QLyThuVien.Application.Features.Authors.Commands.Delete;
using QLyThuVien.Application.Features.Authors.Commands.Update;
using QLyThuVien.Application.Features.Authors.Common;
using QLyThuVien.Application.Features.Authors.Queries;
using QLyThuVien.Application.Features.Categories.Commands.Create;
using QLyThuVien.Application.Features.Categories.Commands.Delete;
using QLyThuVien.Application.Features.Categories.Commands.Update;
using QLyThuVien.Application.Features.Categories.Common;
using QLyThuVien.Application.Features.Categories.Queries;
using QLyThuVien.Application.Features.Publishers.Commands.Create;
using QLyThuVien.Application.Features.Publishers.Commands.Delete;
using QLyThuVien.Application.Features.Publishers.Commands.Update;
using QLyThuVien.Application.Features.Publishers.Common;
using QLyThuVien.Application.Features.Publishers.Queries;

namespace QLyThuVien.Api.Controllers;

[ApiController]
[Route("api/catalog")]
[Authorize]
public sealed class CatalogDictionariesController : ControllerBase
{
    private readonly ISender _sender;

    public CatalogDictionariesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("authors")]
    public Task<IReadOnlyCollection<AuthorDto>> GetAuthors([FromQuery] string? search, CancellationToken cancellationToken)
        => _sender.Send(new GetAuthorsQuery(search), cancellationToken);

    [HttpPost("authors")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Librarian,InventoryStaff")]
    public Task<AuthorDto> CreateAuthor(AuthorRequest request, CancellationToken cancellationToken)
        => _sender.Send(new CreateAuthorCommand(request), cancellationToken);

    [HttpPut("authors/{id:guid}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Librarian,InventoryStaff")]
    public Task<AuthorDto> UpdateAuthor(Guid id, AuthorRequest request, CancellationToken cancellationToken)
        => _sender.Send(new UpdateAuthorCommand(id, request), cancellationToken);

    [HttpDelete("authors/{id:guid}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Librarian,InventoryStaff")]
    public async Task<IActionResult> DeleteAuthor(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteAuthorCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpGet("categories")]
    public Task<IReadOnlyCollection<CategoryDto>> GetCategories([FromQuery] string? search, CancellationToken cancellationToken)
        => _sender.Send(new GetCategoriesQuery(search), cancellationToken);

    [HttpPost("categories")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Librarian,InventoryStaff")]
    public Task<CategoryDto> CreateCategory(CategoryRequest request, CancellationToken cancellationToken)
        => _sender.Send(new CreateCategoryCommand(request), cancellationToken);

    [HttpPut("categories/{id:guid}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Librarian,InventoryStaff")]
    public Task<CategoryDto> UpdateCategory(Guid id, CategoryRequest request, CancellationToken cancellationToken)
        => _sender.Send(new UpdateCategoryCommand(id, request), cancellationToken);

    [HttpDelete("categories/{id:guid}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Librarian,InventoryStaff")]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteCategoryCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpGet("publishers")]
    public Task<IReadOnlyCollection<PublisherDto>> GetPublishers([FromQuery] string? search, CancellationToken cancellationToken)
        => _sender.Send(new GetPublishersQuery(search), cancellationToken);

    [HttpPost("publishers")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Librarian,InventoryStaff")]
    public Task<PublisherDto> CreatePublisher(PublisherRequest request, CancellationToken cancellationToken)
        => _sender.Send(new CreatePublisherCommand(request), cancellationToken);

    [HttpPut("publishers/{id:guid}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Librarian,InventoryStaff")]
    public Task<PublisherDto> UpdatePublisher(Guid id, PublisherRequest request, CancellationToken cancellationToken)
        => _sender.Send(new UpdatePublisherCommand(id, request), cancellationToken);

    [HttpDelete("publishers/{id:guid}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Librarian,InventoryStaff")]
    public async Task<IActionResult> DeletePublisher(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeletePublisherCommand(id), cancellationToken);
        return NoContent();
    }
}
