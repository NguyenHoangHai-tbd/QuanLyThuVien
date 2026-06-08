using QLyThuVien.Application.Interfaces;
using QLyThuVien.Domain.Entities;
using QLyThuVien.Domain.Enums;
using QLyThuVien.Infrastructure.Services;

namespace QLyThuVien.Infrastructure.Persistence;

public sealed class InMemoryLibraryRepository : ILibraryRepository
{
    private readonly List<AiUsageLog> _aiUsageLogs = [];
    private readonly List<AuditLog> _auditLogs = [];
    private readonly List<Author> _authors = [];
    private readonly List<BookCopy> _bookCopies = [];
    private readonly List<Book> _books = [];
    private readonly List<Branch> _branches = [];
    private readonly List<Category> _categories = [];
    private readonly List<HoldRequest> _holds = [];
    private readonly List<LibraryPolicy> _policies = [];
    private readonly List<Loan> _loans = [];
    private readonly List<MemberProfile> _members = [];
    private readonly List<NotificationMessage> _notifications = [];
    private readonly List<Publisher> _publishers = [];
    private readonly List<Tenant> _tenants = [];
    private readonly List<UserAccount> _users = [];

    public InMemoryLibraryRepository()
    {
        Seed();
    }

    public IReadOnlyList<Tenant> Tenants => _tenants;

    public IReadOnlyList<Branch> Branches => _branches;

    public IReadOnlyList<UserAccount> Users => _users;

    public IReadOnlyList<LibraryPolicy> Policies => _policies;

    public IReadOnlyList<Author> Authors => _authors;

    public IReadOnlyList<Category> Categories => _categories;

    public IReadOnlyList<Publisher> Publishers => _publishers;

    public IReadOnlyList<Book> Books => _books;

    public IReadOnlyList<BookCopy> BookCopies => _bookCopies;

    public IReadOnlyList<MemberProfile> Members => _members;

    public IReadOnlyList<Loan> Loans => _loans;

    public IReadOnlyList<HoldRequest> Holds => _holds;

    public IReadOnlyList<NotificationMessage> Notifications => _notifications;

    public IReadOnlyList<AuditLog> AuditLogs => _auditLogs;

    public IReadOnlyList<AiUsageLog> AiUsageLogs => _aiUsageLogs;

    public void AddTenant(Tenant tenant) => _tenants.Add(tenant);

    public void AddBranch(Branch branch) => _branches.Add(branch);

    public void AddUser(UserAccount user) => _users.Add(user);

    public void AddPolicy(LibraryPolicy policy) => _policies.Add(policy);

    public void AddAuthor(Author author) => _authors.Add(author);

    public void AddCategory(Category category) => _categories.Add(category);

    public void AddPublisher(Publisher publisher) => _publishers.Add(publisher);

    public void AddBook(Book book) => _books.Add(book);

    public void AddBookCopy(BookCopy copy) => _bookCopies.Add(copy);

    public void AddMember(MemberProfile member) => _members.Add(member);

    public void AddLoan(Loan loan) => _loans.Add(loan);

    public void AddHold(HoldRequest hold) => _holds.Add(hold);

    public void AddNotification(NotificationMessage notification) => _notifications.Add(notification);

    public void AddAuditLog(AuditLog auditLog) => _auditLogs.Add(auditLog);

    public void AddAiUsageLog(AiUsageLog usageLog) => _aiUsageLogs.Add(usageLog);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    private void Seed()
    {
        var passwordHasher = new Sha256PasswordHasher();
        var now = DateTimeOffset.UtcNow;
        var tenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var mainBranchId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");
        var scienceBranchId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2");
        var adminId = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1");
        var librarianId = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc2");

        var tenant = new Tenant
        {
            Id = tenantId,
            Key = "pacific",
            Name = "Thu vien Dai hoc Thai Binh Duong",
            Plan = "MVP",
            DefaultLocale = "vi",
            PrimaryColor = "#155e75",
            CreatedAt = now.AddDays(-120)
        };
        _tenants.Add(tenant);

        _branches.AddRange([
            new Branch
            {
                Id = mainBranchId,
                TenantId = tenantId,
                Code = "MAIN",
                Name = "Co so chinh",
                Address = "79 Mai Thi Dung, Nha Trang",
                CreatedAt = now.AddDays(-110)
            },
            new Branch
            {
                Id = scienceBranchId,
                TenantId = tenantId,
                Code = "SCI",
                Name = "Kho Khoa hoc - Cong nghe",
                Address = "Tang 3, khu hoc tap",
                CreatedAt = now.AddDays(-100)
            }
        ]);

        _policies.Add(new LibraryPolicy
        {
            TenantId = tenantId,
            MaxLoanDays = 14,
            MaxRenewals = 1,
            DailyFineAmount = 2000,
            MaxActiveLoansPerMember = 5,
            CreatedAt = now.AddDays(-100)
        });

        _users.AddRange([
            new UserAccount
            {
                Id = adminId,
                TenantId = tenantId,
                FullName = "Nguyen Hoang Hai",
                Email = "admin@pacific.edu.vn",
                PasswordHash = passwordHasher.Hash("Admin@123"),
                Role = UserRole.TenantAdmin,
                Locale = "vi",
                CreatedAt = now.AddDays(-90)
            },
            new UserAccount
            {
                Id = librarianId,
                TenantId = tenantId,
                FullName = "Thu thu co so chinh",
                Email = "librarian@pacific.edu.vn",
                PasswordHash = passwordHasher.Hash("Library@123"),
                Role = UserRole.Librarian,
                BranchIds = [mainBranchId],
                Locale = "vi",
                CreatedAt = now.AddDays(-80)
            },
            new UserAccount
            {
                TenantId = tenantId,
                FullName = "Nhan vien kiem ke",
                Email = "inventory@pacific.edu.vn",
                PasswordHash = passwordHasher.Hash("Inventory@123"),
                Role = UserRole.InventoryStaff,
                BranchIds = [mainBranchId, scienceBranchId],
                Locale = "vi",
                CreatedAt = now.AddDays(-70)
            }
        ]);

        var kimDung = AddAuthorSeed(tenantId, "Kim Dung", now);
        var nguyenNhatAnh = AddAuthorSeed(tenantId, "Nguyen Nhat Anh", now);
        var robertMartin = AddAuthorSeed(tenantId, "Robert C. Martin", now);
        var ericEvans = AddAuthorSeed(tenantId, "Eric Evans", now);
        var aiAuthor = AddAuthorSeed(tenantId, "Stuart Russell", now);

        var literature = AddCategorySeed(tenantId, "Van hoc", now);
        var software = AddCategorySeed(tenantId, "Cong nghe phan mem", now);
        var ai = AddCategorySeed(tenantId, "Tri tue nhan tao", now);
        var education = AddCategorySeed(tenantId, "Giao trinh", now);

        var nxbTre = AddPublisherSeed(tenantId, "NXB Tre", now);
        var pearson = AddPublisherSeed(tenantId, "Pearson", now);
        var prentice = AddPublisherSeed(tenantId, "Prentice Hall", now);

        var cleanCode = AddBookSeed(tenantId, "Clean Code", "9780132350884", "Nguyen tac viet ma sach, de doc va de bao tri.", 2008, "en", prentice.Id, [robertMartin.Id], [software.Id], ["clean-code", "software", "best-practice"], now);
        var ddd = AddBookSeed(tenantId, "Domain-Driven Design", "9780321125217", "Thiet ke phan mem theo mien nghiep vu va bounded context.", 2003, "en", pearson.Id, [ericEvans.Id], [software.Id], ["architecture", "domain", "enterprise"], now);
        var aiModern = AddBookSeed(tenantId, "Artificial Intelligence: A Modern Approach", "9780134610993", "Nen tang AI hien dai: search, planning, learning va reasoning.", 2020, "en", pearson.Id, [aiAuthor.Id], [ai.Id, education.Id], ["ai", "machine-learning", "search"], now);
        var matBiec = AddBookSeed(tenantId, "Mat biec", "9786041122334", "Tieu thuyet Viet Nam ve tuoi tre, tinh ban va ky uc.", 1990, "vi", nxbTre.Id, [nguyenNhatAnh.Id], [literature.Id], ["van-hoc", "tuoi-tre"], now);
        var thanDieu = AddBookSeed(tenantId, "Than dieu dai hiep", "9786045566778", "Kiem hiep kinh dien voi cau chuyen ve Duong Qua va Tieu Long Nu.", 1959, "vi", nxbTre.Id, [kimDung.Id], [literature.Id], ["kiem-hiep", "van-hoc"], now);

        AddCopySeed(tenantId, mainBranchId, cleanCode.Id, "PV-MAIN-0001", "Ke A1", BookCopyStatus.OnLoan, now);
        AddCopySeed(tenantId, mainBranchId, cleanCode.Id, "PV-MAIN-0002", "Ke A1", BookCopyStatus.Available, now);
        AddCopySeed(tenantId, scienceBranchId, ddd.Id, "PV-SCI-0003", "Ke S2", BookCopyStatus.Available, now);
        AddCopySeed(tenantId, scienceBranchId, aiModern.Id, "PV-SCI-0004", "Ke AI", BookCopyStatus.OnLoan, now);
        AddCopySeed(tenantId, mainBranchId, matBiec.Id, "PV-MAIN-0005", "Ke V3", BookCopyStatus.Available, now);
        AddCopySeed(tenantId, mainBranchId, thanDieu.Id, "PV-MAIN-0006", "Ke V4", BookCopyStatus.Available, now);

        var memberA = new MemberProfile
        {
            Id = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd1"),
            TenantId = tenantId,
            BranchId = mainBranchId,
            Code = "SV230446",
            FullName = "Nguyen Hoang Hai",
            Email = "230446@student.pacific.edu.vn",
            Phone = "0900000446",
            Status = MemberStatus.Active,
            JoinedAt = now.AddDays(-60),
            CreatedAt = now.AddDays(-60)
        };
        var memberB = new MemberProfile
        {
            Id = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd2"),
            TenantId = tenantId,
            BranchId = scienceBranchId,
            Code = "GV0001",
            FullName = "Tran Minh Khoa",
            Email = "khoa@pacific.edu.vn",
            Phone = "0912000001",
            Status = MemberStatus.Active,
            JoinedAt = now.AddDays(-50),
            CreatedAt = now.AddDays(-50)
        };
        _members.AddRange([memberA, memberB]);

        var cleanCodeCopy = _bookCopies.First(x => x.Barcode == "PV-MAIN-0001");
        var aiCopy = _bookCopies.First(x => x.Barcode == "PV-SCI-0004");
        _loans.AddRange([
            new Loan
            {
                TenantId = tenantId,
                BranchId = mainBranchId,
                MemberId = memberA.Id,
                BookCopyId = cleanCodeCopy.Id,
                LoanedAt = now.AddDays(-5),
                DueAt = now.AddDays(9),
                Status = LoanStatus.Active,
                CreatedAt = now.AddDays(-5),
                CreatedBy = "librarian@pacific.edu.vn"
            },
            new Loan
            {
                TenantId = tenantId,
                BranchId = scienceBranchId,
                MemberId = memberB.Id,
                BookCopyId = aiCopy.Id,
                LoanedAt = now.AddDays(-20),
                DueAt = now.AddDays(-6),
                Status = LoanStatus.Overdue,
                FineAmount = 12000,
                CreatedAt = now.AddDays(-20),
                CreatedBy = "librarian@pacific.edu.vn"
            }
        ]);

        _holds.Add(new HoldRequest
        {
            TenantId = tenantId,
            BranchId = mainBranchId,
            BookId = cleanCode.Id,
            MemberId = memberA.Id,
            Status = HoldStatus.Waiting,
            RequestedAt = now.AddDays(-1),
            CreatedAt = now.AddDays(-1),
            CreatedBy = "admin@pacific.edu.vn"
        });

        _notifications.AddRange([
            new NotificationMessage
            {
                TenantId = tenantId,
                BranchId = mainBranchId,
                MemberId = memberA.Id,
                MessageKey = "loan.due_soon",
                Variables = { ["bookTitle"] = "Clean Code", ["dueAt"] = now.AddDays(9).ToString("yyyy-MM-dd") },
                Status = NotificationStatus.Sent,
                CreatedAt = now.AddHours(-5)
            },
            new NotificationMessage
            {
                TenantId = tenantId,
                BranchId = scienceBranchId,
                MemberId = memberB.Id,
                MessageKey = "loan.overdue",
                Variables = { ["bookTitle"] = "Artificial Intelligence: A Modern Approach", ["fine"] = "12000" },
                Status = NotificationStatus.Queued,
                CreatedAt = now.AddHours(-2)
            }
        ]);

        _auditLogs.AddRange([
            new AuditLog
            {
                TenantId = tenantId,
                BranchId = mainBranchId,
                ActorUserId = librarianId,
                ActorName = "Thu thu co so chinh",
                Action = "circulation.loan.created",
                EntityName = "Loan",
                Summary = "Loaned Clean Code to Nguyen Hoang Hai",
                CreatedAt = now.AddDays(-5)
            },
            new AuditLog
            {
                TenantId = tenantId,
                BranchId = scienceBranchId,
                ActorUserId = adminId,
                ActorName = "Nguyen Hoang Hai",
                Action = "dashboard.viewed",
                EntityName = "Dashboard",
                Summary = "Viewed tenant dashboard and overdue KPI",
                CreatedAt = now.AddHours(-4)
            }
        ]);
    }

    private Author AddAuthorSeed(Guid tenantId, string name, DateTimeOffset now)
    {
        var author = new Author { TenantId = tenantId, Name = name, CreatedAt = now.AddDays(-100) };
        _authors.Add(author);
        return author;
    }

    private Category AddCategorySeed(Guid tenantId, string name, DateTimeOffset now)
    {
        var category = new Category { TenantId = tenantId, Name = name, CreatedAt = now.AddDays(-100) };
        _categories.Add(category);
        return category;
    }

    private Publisher AddPublisherSeed(Guid tenantId, string name, DateTimeOffset now)
    {
        var publisher = new Publisher { TenantId = tenantId, Name = name, CreatedAt = now.AddDays(-100) };
        _publishers.Add(publisher);
        return publisher;
    }

    private Book AddBookSeed(
        Guid tenantId,
        string title,
        string isbn,
        string description,
        int year,
        string language,
        Guid publisherId,
        List<Guid> authorIds,
        List<Guid> categoryIds,
        List<string> tags,
        DateTimeOffset now)
    {
        var book = new Book
        {
            TenantId = tenantId,
            Title = title,
            Isbn = isbn,
            Description = description,
            PublishedYear = year,
            Language = language,
            PublisherId = publisherId,
            AuthorIds = authorIds,
            CategoryIds = categoryIds,
            Tags = tags,
            CreatedAt = now.AddDays(-90)
        };
        _books.Add(book);
        return book;
    }

    private void AddCopySeed(Guid tenantId, Guid branchId, Guid bookId, string barcode, string location, BookCopyStatus status, DateTimeOffset now)
    {
        _bookCopies.Add(new BookCopy
        {
            TenantId = tenantId,
            BranchId = branchId,
            BookId = bookId,
            Barcode = barcode,
            QrCode = $"LIB://pacific/{barcode}",
            Location = location,
            Status = status,
            CreatedAt = now.AddDays(-80)
        });
    }
}
