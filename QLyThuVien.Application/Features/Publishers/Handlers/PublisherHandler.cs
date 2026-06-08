using MediatR;
using QLyThuVien.Application.Common;
using QLyThuVien.Application.Features.Publishers.Commands.Create;
using QLyThuVien.Application.Features.Publishers.Commands.Delete;
using QLyThuVien.Application.Features.Publishers.Commands.Update;
using QLyThuVien.Application.Features.Publishers.Common;
using QLyThuVien.Application.Features.Publishers.Queries;
using QLyThuVien.Application.Interfaces;
using QLyThuVien.Domain.Entities;

namespace QLyThuVien.Application.Features.Publishers.Handlers;

public sealed class PublisherHandler :
    ApplicationRequestHandlerBase,
    IRequestHandler<GetPublishersQuery, IReadOnlyCollection<PublisherDto>>,
    IRequestHandler<CreatePublisherCommand, PublisherDto>,
    IRequestHandler<UpdatePublisherCommand, PublisherDto>,
    IRequestHandler<DeletePublisherCommand>
{
    public PublisherHandler(ILibraryRepository repository, ICurrentUserContext currentUser, IClock clock)
        : base(repository, currentUser, clock)
    {
    }

    public Task<IReadOnlyCollection<PublisherDto>> Handle(GetPublishersQuery query, CancellationToken cancellationToken)
    {
        var search = Clean(query.Search);
        var publishers = TenantScope(Repository.Publishers);

        if (!string.IsNullOrWhiteSpace(search))
        {
            publishers = publishers.Where(x => HasText(x.Name, search));
        }

        var result = publishers
            .OrderBy(x => x.Name)
            .Select(MapPublisher)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<PublisherDto>>(result);
    }

    public async Task<PublisherDto> Handle(CreatePublisherCommand command, CancellationToken cancellationToken)
    {
        EnsureAuthenticated();
        var name = Clean(command.Request.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            throw AppException.BadRequest("Publisher name is required.");
        }

        if (TenantScope(Repository.Publishers).Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            throw AppException.Conflict("Publisher already exists.");
        }

        var publisher = new Publisher
        {
            TenantId = TenantId,
            Name = name,
            CreatedAt = Clock.UtcNow,
            CreatedBy = CurrentUser.Email
        };

        Repository.AddPublisher(publisher);
        AddAudit("catalog.publisher.created", "Publisher", publisher.Id, $"Created publisher {publisher.Name}");
        await Repository.SaveChangesAsync(cancellationToken);

        return MapPublisher(publisher);
    }

    public async Task<PublisherDto> Handle(UpdatePublisherCommand command, CancellationToken cancellationToken)
    {
        EnsureAuthenticated();
        var publisher = TenantScope(Repository.Publishers).FirstOrDefault(x => x.Id == command.Id);
        if (publisher is null)
        {
            throw AppException.NotFound("Publisher not found.");
        }

        var name = Clean(command.Request.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            throw AppException.BadRequest("Publisher name is required.");
        }

        if (TenantScope(Repository.Publishers).Any(x => x.Id != command.Id && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            throw AppException.Conflict("Publisher already exists.");
        }

        publisher.Name = name;
        publisher.UpdatedAt = Clock.UtcNow;
        publisher.UpdatedBy = CurrentUser.Email;

        AddAudit("catalog.publisher.updated", "Publisher", publisher.Id, $"Updated publisher {publisher.Name}");
        await Repository.SaveChangesAsync(cancellationToken);

        return MapPublisher(publisher);
    }

    public async Task Handle(DeletePublisherCommand command, CancellationToken cancellationToken)
    {
        EnsureAuthenticated();
        var publisher = TenantScope(Repository.Publishers).FirstOrDefault(x => x.Id == command.Id);
        if (publisher is null)
        {
            throw AppException.NotFound("Publisher not found.");
        }

        if (TenantScope(Repository.Books).Any(x => x.PublisherId == publisher.Id))
        {
            throw AppException.BadRequest("Cannot delete a publisher that is used by books.");
        }

        publisher.IsDeleted = true;
        publisher.UpdatedAt = Clock.UtcNow;
        publisher.UpdatedBy = CurrentUser.Email;

        AddAudit("catalog.publisher.deleted", "Publisher", publisher.Id, $"Deleted publisher {publisher.Name}");
        await Repository.SaveChangesAsync(cancellationToken);
    }

    private PublisherDto MapPublisher(Publisher publisher)
        => new(publisher.Id, publisher.Name, TenantScope(Repository.Books).Count(x => x.PublisherId == publisher.Id));
}
