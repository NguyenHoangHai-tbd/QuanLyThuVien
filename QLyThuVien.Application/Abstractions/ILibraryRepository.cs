using QLyThuVien.Domain.Entities;

namespace QLyThuVien.Application.Abstractions;

public interface ILibraryRepository
{
    IReadOnlyList<Tenant> Tenants { get; }

    IReadOnlyList<Branch> Branches { get; }

    IReadOnlyList<UserAccount> Users { get; }

    IReadOnlyList<LibraryPolicy> Policies { get; }

    IReadOnlyList<Author> Authors { get; }

    IReadOnlyList<Category> Categories { get; }

    IReadOnlyList<Publisher> Publishers { get; }

    IReadOnlyList<Book> Books { get; }

    IReadOnlyList<BookCopy> BookCopies { get; }

    IReadOnlyList<MemberProfile> Members { get; }

    IReadOnlyList<Loan> Loans { get; }

    IReadOnlyList<HoldRequest> Holds { get; }

    IReadOnlyList<NotificationMessage> Notifications { get; }

    IReadOnlyList<AuditLog> AuditLogs { get; }

    IReadOnlyList<AiUsageLog> AiUsageLogs { get; }

    void AddTenant(Tenant tenant);

    void AddBranch(Branch branch);

    void AddUser(UserAccount user);

    void AddPolicy(LibraryPolicy policy);

    void AddAuthor(Author author);

    void AddCategory(Category category);

    void AddPublisher(Publisher publisher);

    void AddBook(Book book);

    void AddBookCopy(BookCopy copy);

    void AddMember(MemberProfile member);

    void AddLoan(Loan loan);

    void AddHold(HoldRequest hold);

    void AddNotification(NotificationMessage notification);

    void AddAuditLog(AuditLog auditLog);

    void AddAiUsageLog(AiUsageLog usageLog);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
