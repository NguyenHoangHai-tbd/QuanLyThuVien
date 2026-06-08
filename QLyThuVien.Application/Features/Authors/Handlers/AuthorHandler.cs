using MediatR;
using QLyThuVien.Application.Common;
using QLyThuVien.Application.Features.Authors.Commands.Create;
using QLyThuVien.Application.Features.Authors.Commands.Delete;
using QLyThuVien.Application.Features.Authors.Commands.Update;
using QLyThuVien.Application.Features.Authors.Common;
using QLyThuVien.Application.Features.Authors.Queries;
using QLyThuVien.Application.Interfaces;
using QLyThuVien.Domain.Entities;

namespace QLyThuVien.Application.Features.Authors.Handlers;

public sealed class AuthorHandler :
    ApplicationRequestHandlerBase,
    IRequestHandler<GetAuthorsQuery, IReadOnlyCollection<AuthorDto>>,
    IRequestHandler<CreateAuthorCommand, AuthorDto>,
    IRequestHandler<UpdateAuthorCommand, AuthorDto>,
    IRequestHandler<DeleteAuthorCommand>
{
    public AuthorHandler(ILibraryRepository repository, ICurrentUserContext currentUser, IClock clock)
        : base(repository, currentUser, clock)
    {
    }

    public Task<IReadOnlyCollection<AuthorDto>> Handle(GetAuthorsQuery query, CancellationToken cancellationToken)
    {
        var search = Clean(query.Search);
        var authors = TenantScope(Repository.Authors);

        if (!string.IsNullOrWhiteSpace(search))
        {
            authors = authors.Where(x => HasText(x.Name, search));
        }

        var result = authors
            .OrderBy(x => x.Name)
            .Select(MapAuthor)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<AuthorDto>>(result);
    }

    public async Task<AuthorDto> Handle(CreateAuthorCommand command, CancellationToken cancellationToken)
    {
        EnsureAuthenticated();
        var name = Clean(command.Request.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            throw AppException.BadRequest("Author name is required.");
        }

        if (TenantScope(Repository.Authors).Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            throw AppException.Conflict("Author already exists.");
        }

        var author = new Author
        {
            TenantId = TenantId,
            Name = name,
            CreatedAt = Clock.UtcNow,
            CreatedBy = CurrentUser.Email
        };

        Repository.AddAuthor(author);
        AddAudit("catalog.author.created", "Author", author.Id, $"Created author {author.Name}");
        await Repository.SaveChangesAsync(cancellationToken);

        return MapAuthor(author);
    }

    public async Task<AuthorDto> Handle(UpdateAuthorCommand command, CancellationToken cancellationToken)
    {
        EnsureAuthenticated();
        var author = TenantScope(Repository.Authors).FirstOrDefault(x => x.Id == command.Id);
        if (author is null)
        {
            throw AppException.NotFound("Author not found.");
        }

        var name = Clean(command.Request.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            throw AppException.BadRequest("Author name is required.");
        }

        if (TenantScope(Repository.Authors).Any(x => x.Id != command.Id && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            throw AppException.Conflict("Author already exists.");
        }

        author.Name = name;
        author.UpdatedAt = Clock.UtcNow;
        author.UpdatedBy = CurrentUser.Email;

        AddAudit("catalog.author.updated", "Author", author.Id, $"Updated author {author.Name}");
        await Repository.SaveChangesAsync(cancellationToken);

        return MapAuthor(author);
    }

    public async Task Handle(DeleteAuthorCommand command, CancellationToken cancellationToken)
    {
        EnsureAuthenticated();
        var author = TenantScope(Repository.Authors).FirstOrDefault(x => x.Id == command.Id);
        if (author is null)
        {
            throw AppException.NotFound("Author not found.");
        }

        if (TenantScope(Repository.Books).Any(x => x.AuthorIds.Contains(author.Id)))
        {
            throw AppException.BadRequest("Cannot delete an author that is used by books.");
        }

        author.IsDeleted = true;
        author.UpdatedAt = Clock.UtcNow;
        author.UpdatedBy = CurrentUser.Email;

        AddAudit("catalog.author.deleted", "Author", author.Id, $"Deleted author {author.Name}");
        await Repository.SaveChangesAsync(cancellationToken);
    }

    private AuthorDto MapAuthor(Author author)
        => new(author.Id, author.Name, TenantScope(Repository.Books).Count(x => x.AuthorIds.Contains(author.Id)));
}
