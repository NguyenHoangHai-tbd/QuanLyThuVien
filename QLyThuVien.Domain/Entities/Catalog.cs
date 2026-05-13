using QLyThuVien.Domain.Common;
using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Domain.Entities;

public sealed class Author : TenantEntity
{
    public string Name { get; set; } = string.Empty;
}

public sealed class Category : TenantEntity
{
    public string Name { get; set; } = string.Empty;
}

public sealed class Publisher : TenantEntity
{
    public string Name { get; set; } = string.Empty;
}

public sealed class Book : TenantEntity
{
    public string Title { get; set; } = string.Empty;

    public string Isbn { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int? PublishedYear { get; set; }

    public string Language { get; set; } = "vi";

    public Guid? PublisherId { get; set; }

    public List<Guid> AuthorIds { get; set; } = [];

    public List<Guid> CategoryIds { get; set; } = [];

    public List<string> Tags { get; set; } = [];

    public string CoverUrl { get; set; } = string.Empty;
}

public sealed class BookCopy : BranchEntity
{
    public Guid BookId { get; set; }

    public string Barcode { get; set; } = string.Empty;

    public string QrCode { get; set; } = string.Empty;

    public BookCopyStatus Status { get; set; } = BookCopyStatus.Available;

    public string Location { get; set; } = string.Empty;
}
