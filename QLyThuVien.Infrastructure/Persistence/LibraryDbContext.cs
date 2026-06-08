using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using QLyThuVien.Domain.Entities;

namespace QLyThuVien.Infrastructure.Persistence;

public sealed class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options)
        : base(options)
    {
    }

    public DbSet<AiUsageLog> AiUsageLogs => Set<AiUsageLog>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<Author> Authors => Set<Author>();

    public DbSet<Book> Books => Set<Book>();

    public DbSet<BookCopy> BookCopies => Set<BookCopy>();

    public DbSet<Branch> Branches => Set<Branch>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<HoldRequest> HoldRequests => Set<HoldRequest>();

    public DbSet<LibraryPolicy> LibraryPolicies => Set<LibraryPolicy>();

    public DbSet<Loan> Loans => Set<Loan>();

    public DbSet<MemberProfile> MemberProfiles => Set<MemberProfile>();

    public DbSet<NotificationMessage> NotificationMessages => Set<NotificationMessage>();

    public DbSet<Publisher> Publishers => Set<Publisher>();

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureBaseEntities(modelBuilder);
        ConfigureSaaS(modelBuilder);
        ConfigureIdentity(modelBuilder);
        ConfigureCatalog(modelBuilder);
        ConfigureCirculation(modelBuilder);
        ConfigureOperations(modelBuilder);
    }

    private static void ConfigureBaseEntities(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(Entity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).HasKey(nameof(Entity.Id));
            }

            if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).Property(nameof(AuditableEntity.CreatedBy)).HasMaxLength(160);
                modelBuilder.Entity(entityType.ClrType).Property(nameof(AuditableEntity.UpdatedBy)).HasMaxLength(160);
            }
        }
    }

    private static void ConfigureSaaS(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("Tenants");
            entity.HasIndex(x => x.Key).IsUnique();
            entity.Property(x => x.Key).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(220).IsRequired();
            entity.Property(x => x.Plan).HasMaxLength(80);
            entity.Property(x => x.DefaultLocale).HasMaxLength(10);
            entity.Property(x => x.PrimaryColor).HasMaxLength(30);
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.ToTable("Branches");
            entity.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(220).IsRequired();
            entity.Property(x => x.Address).HasMaxLength(500);
        });

        modelBuilder.Entity<LibraryPolicy>(entity =>
        {
            entity.ToTable("LibraryPolicies");
            entity.HasIndex(x => x.TenantId).IsUnique();
            entity.Property(x => x.DailyFineAmount).HasColumnType("decimal(18,2)");
        });
    }

    private static void ConfigureIdentity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("UserAccounts");
            entity.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
            entity.Property(x => x.FullName).HasMaxLength(220).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(220).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(x => x.Locale).HasMaxLength(10);
            entity.Property(x => x.BranchIds)
                .HasConversion(GuidListConverter())
                .Metadata.SetValueComparer(GuidListComparer());
        });
    }

    private static void ConfigureCatalog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Author>(entity =>
        {
            entity.ToTable("Authors");
            entity.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(220).IsRequired();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(220).IsRequired();
        });

        modelBuilder.Entity<Publisher>(entity =>
        {
            entity.ToTable("Publishers");
            entity.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(220).IsRequired();
        });

        modelBuilder.Entity<Book>(entity =>
        {
            entity.ToTable("Books");
            entity.HasIndex(x => new { x.TenantId, x.Isbn }).IsUnique();
            entity.Property(x => x.Title).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Isbn).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.Language).HasMaxLength(10);
            entity.Property(x => x.CoverUrl).HasMaxLength(1000);
            entity.Property(x => x.AuthorIds)
                .HasConversion(GuidListConverter())
                .Metadata.SetValueComparer(GuidListComparer());
            entity.Property(x => x.CategoryIds)
                .HasConversion(GuidListConverter())
                .Metadata.SetValueComparer(GuidListComparer());
            entity.Property(x => x.Tags)
                .HasConversion(StringListConverter())
                .Metadata.SetValueComparer(StringListComparer());
        });

        modelBuilder.Entity<BookCopy>(entity =>
        {
            entity.ToTable("BookCopies");
            entity.HasIndex(x => new { x.TenantId, x.Barcode }).IsUnique();
            entity.Property(x => x.Barcode).HasMaxLength(120).IsRequired();
            entity.Property(x => x.QrCode).HasMaxLength(260);
            entity.Property(x => x.Location).HasMaxLength(220);
        });
    }

    private static void ConfigureCirculation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MemberProfile>(entity =>
        {
            entity.ToTable("MemberProfiles");
            entity.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(80).IsRequired();
            entity.Property(x => x.FullName).HasMaxLength(220).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(220);
            entity.Property(x => x.Phone).HasMaxLength(40);
        });

        modelBuilder.Entity<Loan>(entity =>
        {
            entity.ToTable("Loans");
            entity.Property(x => x.FineAmount).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<HoldRequest>(entity =>
        {
            entity.ToTable("HoldRequests");
        });
    }

    private static void ConfigureOperations(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationMessage>(entity =>
        {
            entity.ToTable("NotificationMessages");
            entity.Property(x => x.Channel).HasMaxLength(80);
            entity.Property(x => x.MessageKey).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Variables)
                .HasConversion(DictionaryConverter())
                .Metadata.SetValueComparer(DictionaryComparer());
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.Property(x => x.ActorName).HasMaxLength(220);
            entity.Property(x => x.Action).HasMaxLength(160).IsRequired();
            entity.Property(x => x.EntityName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Summary).HasMaxLength(1000);
        });

        modelBuilder.Entity<AiUsageLog>(entity =>
        {
            entity.ToTable("AiUsageLogs");
            entity.Property(x => x.Feature).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Prompt).HasMaxLength(2000);
        });
    }

    private static ValueConverter<List<Guid>, string> GuidListConverter()
        => new(
            value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
            value => JsonSerializer.Deserialize<List<Guid>>(value, (JsonSerializerOptions?)null) ?? new List<Guid>());

    private static ValueComparer<List<Guid>> GuidListComparer()
        => new(
            (left, right) => left != null && right != null && left.SequenceEqual(right),
            value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
            value => value.ToList());

    private static ValueConverter<List<string>, string> StringListConverter()
        => new(
            value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
            value => JsonSerializer.Deserialize<List<string>>(value, (JsonSerializerOptions?)null) ?? new List<string>());

    private static ValueComparer<List<string>> StringListComparer()
        => new(
            (left, right) => left != null && right != null && left.SequenceEqual(right),
            value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode(StringComparison.Ordinal))),
            value => value.ToList());

    private static ValueConverter<Dictionary<string, string>, string> DictionaryConverter()
        => new(
            value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
            value => JsonSerializer.Deserialize<Dictionary<string, string>>(value, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>());

    private static ValueComparer<Dictionary<string, string>> DictionaryComparer()
        => new(
            (left, right) => left != null && right != null && left.OrderBy(x => x.Key).SequenceEqual(right.OrderBy(x => x.Key)),
            value => value.OrderBy(x => x.Key).Aggregate(0, (hash, item) => HashCode.Combine(hash, item.Key.GetHashCode(StringComparison.Ordinal), item.Value.GetHashCode(StringComparison.Ordinal))),
            value => value.ToDictionary(x => x.Key, x => x.Value));
}
