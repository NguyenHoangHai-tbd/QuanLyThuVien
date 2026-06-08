using MediatR;
using QLyThuVien.Application.Common;
using QLyThuVien.Application.Features.Categories.Commands.Create;
using QLyThuVien.Application.Features.Categories.Commands.Delete;
using QLyThuVien.Application.Features.Categories.Commands.Update;
using QLyThuVien.Application.Features.Categories.Common;
using QLyThuVien.Application.Features.Categories.Queries;
using QLyThuVien.Application.Interfaces;
using QLyThuVien.Domain.Entities;

namespace QLyThuVien.Application.Features.Categories.Handlers;

public sealed class CategoryHandler :
    ApplicationRequestHandlerBase,
    IRequestHandler<GetCategoriesQuery, IReadOnlyCollection<CategoryDto>>,
    IRequestHandler<CreateCategoryCommand, CategoryDto>,
    IRequestHandler<UpdateCategoryCommand, CategoryDto>,
    IRequestHandler<DeleteCategoryCommand>
{
    public CategoryHandler(ILibraryRepository repository, ICurrentUserContext currentUser, IClock clock)
        : base(repository, currentUser, clock)
    {
    }

    public Task<IReadOnlyCollection<CategoryDto>> Handle(GetCategoriesQuery query, CancellationToken cancellationToken)
    {
        var search = Clean(query.Search);
        var categories = TenantScope(Repository.Categories);

        if (!string.IsNullOrWhiteSpace(search))
        {
            categories = categories.Where(x => HasText(x.Name, search));
        }

        var result = categories
            .OrderBy(x => x.Name)
            .Select(MapCategory)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<CategoryDto>>(result);
    }

    public async Task<CategoryDto> Handle(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        EnsureAuthenticated();
        var name = Clean(command.Request.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            throw AppException.BadRequest("Category name is required.");
        }

        if (TenantScope(Repository.Categories).Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            throw AppException.Conflict("Category already exists.");
        }

        var category = new Category
        {
            TenantId = TenantId,
            Name = name,
            CreatedAt = Clock.UtcNow,
            CreatedBy = CurrentUser.Email
        };

        Repository.AddCategory(category);
        AddAudit("catalog.category.created", "Category", category.Id, $"Created category {category.Name}");
        await Repository.SaveChangesAsync(cancellationToken);

        return MapCategory(category);
    }

    public async Task<CategoryDto> Handle(UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        EnsureAuthenticated();
        var category = TenantScope(Repository.Categories).FirstOrDefault(x => x.Id == command.Id);
        if (category is null)
        {
            throw AppException.NotFound("Category not found.");
        }

        var name = Clean(command.Request.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            throw AppException.BadRequest("Category name is required.");
        }

        if (TenantScope(Repository.Categories).Any(x => x.Id != command.Id && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            throw AppException.Conflict("Category already exists.");
        }

        category.Name = name;
        category.UpdatedAt = Clock.UtcNow;
        category.UpdatedBy = CurrentUser.Email;

        AddAudit("catalog.category.updated", "Category", category.Id, $"Updated category {category.Name}");
        await Repository.SaveChangesAsync(cancellationToken);

        return MapCategory(category);
    }

    public async Task Handle(DeleteCategoryCommand command, CancellationToken cancellationToken)
    {
        EnsureAuthenticated();
        var category = TenantScope(Repository.Categories).FirstOrDefault(x => x.Id == command.Id);
        if (category is null)
        {
            throw AppException.NotFound("Category not found.");
        }

        if (TenantScope(Repository.Books).Any(x => x.CategoryIds.Contains(category.Id)))
        {
            throw AppException.BadRequest("Cannot delete a category that is used by books.");
        }

        category.IsDeleted = true;
        category.UpdatedAt = Clock.UtcNow;
        category.UpdatedBy = CurrentUser.Email;

        AddAudit("catalog.category.deleted", "Category", category.Id, $"Deleted category {category.Name}");
        await Repository.SaveChangesAsync(cancellationToken);
    }

    private CategoryDto MapCategory(Category category)
        => new(category.Id, category.Name, TenantScope(Repository.Books).Count(x => x.CategoryIds.Contains(category.Id)));
}
