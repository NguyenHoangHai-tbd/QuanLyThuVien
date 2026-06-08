using MediatR;
using QLyThuVien.Application.Common;
using QLyThuVien.Application.Features.Holds.Commands.Cancel;
using QLyThuVien.Application.Features.Holds.Commands.Create;
using QLyThuVien.Application.Features.Holds.Common;
using QLyThuVien.Application.Features.Holds.Queries;
using QLyThuVien.Application.Interfaces;
using QLyThuVien.Domain.Entities;
using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Features.Holds.Handlers;

public sealed class HoldsHandler :
    ApplicationRequestHandlerBase,
    IRequestHandler<GetHoldsQuery, IReadOnlyCollection<HoldDto>>,
    IRequestHandler<CreateHoldCommand, HoldDto>,
    IRequestHandler<CancelHoldCommand, HoldDto>
{
    public HoldsHandler(ILibraryRepository repository, ICurrentUserContext currentUser, IClock clock)
        : base(repository, currentUser, clock)
    {
    }

    public Task<IReadOnlyCollection<HoldDto>> Handle(GetHoldsQuery query, CancellationToken cancellationToken)
    {
        var holds = BranchScope(Repository.Holds);
        if (query.BranchId.HasValue)
        {
            EnsureBranchAccess(query.BranchId.Value);
            holds = holds.Where(x => x.BranchId == query.BranchId.Value);
        }

        var result = holds.OrderByDescending(x => x.RequestedAt).Select(MapHold).ToArray();
        return Task.FromResult<IReadOnlyCollection<HoldDto>>(result);
    }

    public async Task<HoldDto> Handle(CreateHoldCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var branch = GetBranch(request.BranchId);
        var book = TenantScope(Repository.Books).FirstOrDefault(x => x.Id == request.BookId);
        if (book is null)
        {
            throw AppException.NotFound("Book not found.");
        }

        var member = BranchScope(Repository.Members).FirstOrDefault(x => x.Id == request.MemberId && x.BranchId == branch.Id);
        if (member is null)
        {
            throw AppException.NotFound("Member not found in this branch.");
        }

        var hasAvailableCopy = BranchScope(Repository.BookCopies).Any(x =>
            x.BookId == book.Id &&
            x.BranchId == branch.Id &&
            x.Status == BookCopyStatus.Available);

        var hold = new HoldRequest
        {
            TenantId = TenantId,
            BranchId = branch.Id,
            BookId = book.Id,
            MemberId = member.Id,
            Status = hasAvailableCopy ? HoldStatus.Ready : HoldStatus.Waiting,
            RequestedAt = Clock.UtcNow,
            ExpiresAt = hasAvailableCopy ? Clock.UtcNow.AddDays(3) : null,
            CreatedAt = Clock.UtcNow,
            CreatedBy = CurrentUser.Email
        };

        Repository.AddHold(hold);
        AddAudit("circulation.hold.created", "HoldRequest", hold.Id, $"Created hold for {book.Title}", branch.Id);
        await Repository.SaveChangesAsync(cancellationToken);

        return MapHold(hold);
    }

    public async Task<HoldDto> Handle(CancelHoldCommand command, CancellationToken cancellationToken)
    {
        var hold = BranchScope(Repository.Holds).FirstOrDefault(x => x.Id == command.Id);
        if (hold is null)
        {
            throw AppException.NotFound("Hold request not found.");
        }

        if (hold.Status is not (HoldStatus.Waiting or HoldStatus.Ready))
        {
            throw AppException.BadRequest("Only waiting or ready holds can be cancelled.");
        }

        hold.Status = HoldStatus.Cancelled;
        hold.ExpiresAt = null;
        hold.UpdatedAt = Clock.UtcNow;
        hold.UpdatedBy = CurrentUser.Email;

        AddAudit("circulation.hold.cancelled", "HoldRequest", hold.Id, "Cancelled hold request", hold.BranchId);
        await Repository.SaveChangesAsync(cancellationToken);

        return MapHold(hold);
    }

    private HoldDto MapHold(HoldRequest hold)
    {
        var book = Repository.Books.FirstOrDefault(x => x.Id == hold.BookId);
        var member = Repository.Members.FirstOrDefault(x => x.Id == hold.MemberId);
        return new HoldDto(
            hold.Id,
            hold.BookId,
            book?.Title ?? string.Empty,
            hold.MemberId,
            member?.FullName ?? string.Empty,
            hold.BranchId,
            hold.Status,
            hold.RequestedAt,
            hold.ExpiresAt);
    }
}
