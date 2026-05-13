using QLyThuVien.Application.Abstractions;
using QLyThuVien.Application.Common;
using QLyThuVien.Application.Dtos;
using QLyThuVien.Domain.Entities;
using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Services;

public sealed class CirculationService : ApplicationServiceBase
{
    public CirculationService(ILibraryRepository repository, ICurrentUserContext currentUser, IClock clock)
        : base(repository, currentUser, clock)
    {
    }

    public Task<IReadOnlyCollection<LoanDto>> GetLoansAsync(bool activeOnly, Guid? branchId, CancellationToken cancellationToken = default)
    {
        RefreshOverdueLoans();
        var loans = BranchScope(Repository.Loans);

        if (activeOnly)
        {
            loans = loans.Where(x => x.Status is LoanStatus.Active or LoanStatus.Overdue);
        }

        if (branchId.HasValue)
        {
            EnsureBranchAccess(branchId.Value);
            loans = loans.Where(x => x.BranchId == branchId.Value);
        }

        var result = loans
            .OrderByDescending(x => x.LoanedAt)
            .Select(MapLoan)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<LoanDto>>(result);
    }

    public async Task<LoanDto> CreateLoanAsync(LoanRequest request, CancellationToken cancellationToken = default)
    {
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

    public async Task<LoanDto> ReturnAsync(ReturnRequest request, CancellationToken cancellationToken = default)
    {
        RefreshOverdueLoans();

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

    public async Task<LoanDto> RenewAsync(RenewRequest request, CancellationToken cancellationToken = default)
    {
        RefreshOverdueLoans();

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

    public async Task<HoldDto> CreateHoldAsync(HoldRequestPayload request, CancellationToken cancellationToken = default)
    {
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

    public Task<IReadOnlyCollection<HoldDto>> GetHoldsAsync(Guid? branchId, CancellationToken cancellationToken = default)
    {
        var holds = BranchScope(Repository.Holds);
        if (branchId.HasValue)
        {
            EnsureBranchAccess(branchId.Value);
            holds = holds.Where(x => x.BranchId == branchId.Value);
        }

        var result = holds.OrderByDescending(x => x.RequestedAt).Select(MapHold).ToArray();
        return Task.FromResult<IReadOnlyCollection<HoldDto>>(result);
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
