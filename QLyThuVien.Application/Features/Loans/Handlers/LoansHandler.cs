using MediatR;
using QLyThuVien.Application.Common;
using QLyThuVien.Application.Features.Loans.Commands.Create;
using QLyThuVien.Application.Features.Loans.Commands.Renew;
using QLyThuVien.Application.Features.Loans.Commands.Return;
using QLyThuVien.Application.Features.Loans.Common;
using QLyThuVien.Application.Features.Loans.Queries;
using QLyThuVien.Application.Interfaces;
using QLyThuVien.Domain.Entities;
using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Features.Loans.Handlers;

public sealed class LoansHandler :
    ApplicationRequestHandlerBase,
    IRequestHandler<GetLoansQuery, IReadOnlyCollection<LoanDto>>,
    IRequestHandler<CreateLoanCommand, LoanDto>,
    IRequestHandler<ReturnLoanCommand, LoanDto>,
    IRequestHandler<RenewLoanCommand, LoanDto>
{
    public LoansHandler(ILibraryRepository repository, ICurrentUserContext currentUser, IClock clock)
        : base(repository, currentUser, clock)
    {
    }

    public Task<IReadOnlyCollection<LoanDto>> Handle(GetLoansQuery query, CancellationToken cancellationToken)
    {
        RefreshOverdueLoans();
        var loans = BranchScope(Repository.Loans);

        if (query.ActiveOnly)
        {
            loans = loans.Where(x => x.Status is LoanStatus.Active or LoanStatus.Overdue);
        }

        if (query.BranchId.HasValue)
        {
            EnsureBranchAccess(query.BranchId.Value);
            loans = loans.Where(x => x.BranchId == query.BranchId.Value);
        }

        var result = loans
            .OrderByDescending(x => x.LoanedAt)
            .Select(MapLoan)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<LoanDto>>(result);
    }

    public async Task<LoanDto> Handle(CreateLoanCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var branch = GetBranch(request.BranchId);
        var member = BranchScope(Repository.Members).FirstOrDefault(x => x.Id == request.MemberId && x.BranchId == branch.Id);
        if (member is null)
        {
            throw AppException.NotFound("Member not found in this branch.");
        }

        if (member.Status != MemberStatus.Active)
        {
            throw AppException.BadRequest("Member is not active.");
        }

        var copy = BranchScope(Repository.BookCopies).FirstOrDefault(x =>
            x.BranchId == branch.Id &&
            x.Barcode.Equals(Clean(request.CopyBarcode), StringComparison.OrdinalIgnoreCase));

        if (copy is null)
        {
            throw AppException.NotFound("Book copy not found.");
        }

        if (copy.Status != BookCopyStatus.Available)
        {
            throw AppException.BadRequest("Book copy is not available.");
        }

        var policy = GetPolicy();
        var activeLoanCount = TenantScope(Repository.Loans).Count(x =>
            x.MemberId == member.Id &&
            x.Status is LoanStatus.Active or LoanStatus.Overdue);

        if (activeLoanCount >= policy.MaxActiveLoansPerMember)
        {
            throw AppException.BadRequest("Member has reached the active loan limit.");
        }

        var loan = new Loan
        {
            TenantId = TenantId,
            BranchId = branch.Id,
            MemberId = member.Id,
            BookCopyId = copy.Id,
            LoanedAt = Clock.UtcNow,
            DueAt = Clock.UtcNow.AddDays(policy.MaxLoanDays),
            CreatedAt = Clock.UtcNow,
            CreatedBy = CurrentUser.Email
        };

        copy.Status = BookCopyStatus.OnLoan;
        copy.UpdatedAt = Clock.UtcNow;
        copy.UpdatedBy = CurrentUser.Email;

        Repository.AddLoan(loan);
        Repository.AddNotification(new NotificationMessage
        {
            TenantId = TenantId,
            BranchId = branch.Id,
            MemberId = member.Id,
            MessageKey = "loan.created",
            Variables =
            {
                ["memberName"] = member.FullName,
                ["dueAt"] = loan.DueAt.ToString("yyyy-MM-dd")
            },
            CreatedAt = Clock.UtcNow,
            CreatedBy = CurrentUser.Email
        });
        AddAudit("circulation.loan.created", "Loan", loan.Id, $"Loaned copy {copy.Barcode} to {member.FullName}", branch.Id);
        await Repository.SaveChangesAsync(cancellationToken);

        return MapLoan(loan);
    }

    public async Task<LoanDto> Handle(ReturnLoanCommand command, CancellationToken cancellationToken)
    {
        RefreshOverdueLoans();
        var request = command.Request;
        var copy = BranchScope(Repository.BookCopies).FirstOrDefault(x =>
            x.Barcode.Equals(Clean(request.CopyBarcode), StringComparison.OrdinalIgnoreCase));

        if (copy is null)
        {
            throw AppException.NotFound("Book copy not found.");
        }

        var loan = BranchScope(Repository.Loans).FirstOrDefault(x =>
            x.BookCopyId == copy.Id &&
            x.Status is LoanStatus.Active or LoanStatus.Overdue);

        if (loan is null)
        {
            throw AppException.NotFound("Active loan not found for this barcode.");
        }

        loan.ReturnedAt = Clock.UtcNow;
        loan.Status = LoanStatus.Returned;
        loan.UpdatedAt = Clock.UtcNow;
        loan.UpdatedBy = CurrentUser.Email;

        if (Clock.UtcNow > loan.DueAt)
        {
            var lateDays = Math.Max(0, (Clock.UtcNow.Date - loan.DueAt.Date).Days);
            loan.FineAmount = lateDays * GetPolicy().DailyFineAmount;
        }

        copy.Status = BookCopyStatus.Available;
        copy.UpdatedAt = Clock.UtcNow;
        copy.UpdatedBy = CurrentUser.Email;

        MarkFirstHoldReady(copy);
        AddAudit("circulation.loan.returned", "Loan", loan.Id, $"Returned copy {copy.Barcode}", copy.BranchId);
        await Repository.SaveChangesAsync(cancellationToken);

        return MapLoan(loan);
    }

    public async Task<LoanDto> Handle(RenewLoanCommand command, CancellationToken cancellationToken)
    {
        RefreshOverdueLoans();
        var request = command.Request;
        var loan = BranchScope(Repository.Loans).FirstOrDefault(x => x.Id == request.LoanId);
        if (loan is null)
        {
            throw AppException.NotFound("Loan not found.");
        }

        if (loan.Status != LoanStatus.Active)
        {
            throw AppException.BadRequest("Only active, non-overdue loans can be renewed.");
        }

        var policy = GetPolicy();
        if (loan.RenewalCount >= policy.MaxRenewals)
        {
            throw AppException.BadRequest("Renewal limit reached.");
        }

        loan.RenewalCount++;
        loan.DueAt = loan.DueAt.AddDays(policy.MaxLoanDays);
        loan.UpdatedAt = Clock.UtcNow;
        loan.UpdatedBy = CurrentUser.Email;

        AddAudit("circulation.loan.renewed", "Loan", loan.Id, $"Renewed loan until {loan.DueAt:yyyy-MM-dd}", loan.BranchId);
        await Repository.SaveChangesAsync(cancellationToken);

        return MapLoan(loan);
    }

    private void RefreshOverdueLoans()
    {
        foreach (var loan in TenantScope(Repository.Loans).Where(x => x.Status == LoanStatus.Active && x.DueAt < Clock.UtcNow))
        {
            loan.Status = LoanStatus.Overdue;
        }
    }

    private void MarkFirstHoldReady(BookCopy copy)
    {
        var waitingHold = TenantScope(Repository.Holds)
            .Where(x => x.BookId == copy.BookId && x.BranchId == copy.BranchId && x.Status == HoldStatus.Waiting)
            .OrderBy(x => x.RequestedAt)
            .FirstOrDefault();

        if (waitingHold is null)
        {
            return;
        }

        waitingHold.Status = HoldStatus.Ready;
        waitingHold.ExpiresAt = Clock.UtcNow.AddDays(3);
        waitingHold.UpdatedAt = Clock.UtcNow;
        waitingHold.UpdatedBy = CurrentUser.Email;

        Repository.AddNotification(new NotificationMessage
        {
            TenantId = TenantId,
            BranchId = waitingHold.BranchId,
            MemberId = waitingHold.MemberId,
            MessageKey = "hold.ready",
            Variables =
            {
                ["bookId"] = waitingHold.BookId.ToString(),
                ["expiresAt"] = waitingHold.ExpiresAt.Value.ToString("yyyy-MM-dd")
            },
            CreatedAt = Clock.UtcNow,
            CreatedBy = CurrentUser.Email
        });
    }

    private LibraryPolicy GetPolicy()
        => Repository.Policies.FirstOrDefault(x => x.TenantId == TenantId && !x.IsDeleted)
            ?? new LibraryPolicy { TenantId = TenantId };

    private LoanDto MapLoan(Loan loan)
    {
        var member = Repository.Members.FirstOrDefault(x => x.Id == loan.MemberId);
        var copy = Repository.BookCopies.FirstOrDefault(x => x.Id == loan.BookCopyId);
        var book = copy is null ? null : Repository.Books.FirstOrDefault(x => x.Id == copy.BookId);
        var branch = Repository.Branches.FirstOrDefault(x => x.Id == loan.BranchId);

        return new LoanDto(
            loan.Id,
            loan.MemberId,
            member?.FullName ?? string.Empty,
            loan.BookCopyId,
            copy?.Barcode ?? string.Empty,
            book?.Title ?? string.Empty,
            loan.BranchId,
            branch?.Name ?? string.Empty,
            loan.LoanedAt,
            loan.DueAt,
            loan.ReturnedAt,
            loan.Status,
            loan.RenewalCount,
            loan.FineAmount);
    }
}
