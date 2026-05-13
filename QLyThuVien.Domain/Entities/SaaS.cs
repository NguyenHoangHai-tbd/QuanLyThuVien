using QLyThuVien.Domain.Common;

namespace QLyThuVien.Domain.Entities;

public sealed class Tenant : AuditableEntity
{
    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Plan { get; set; } = "MVP";

    public string DefaultLocale { get; set; } = "vi";

    public bool IsActive { get; set; } = true;

    public string PrimaryColor { get; set; } = "#155e75";
}

public sealed class Branch : TenantEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public sealed class LibraryPolicy : TenantEntity
{
    public int MaxLoanDays { get; set; } = 14;

    public int MaxRenewals { get; set; } = 1;

    public decimal DailyFineAmount { get; set; } = 2000;

    public int MaxActiveLoansPerMember { get; set; } = 5;
}
