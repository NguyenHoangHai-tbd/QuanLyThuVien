using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Dtos;

public sealed record UserContextDto(
    Guid UserId,
    string FullName,
    string Email,
    UserRole Role,
    Guid TenantId,
    string TenantKey,
    string TenantName,
    IReadOnlyCollection<Guid> BranchIds,
    string Locale);

public sealed record LoginRequest(string TenantKey, string Email, string Password);

public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, UserContextDto User);

public sealed record TenantDto(Guid Id, string Key, string Name, string Plan, string DefaultLocale, string PrimaryColor, bool IsActive);

public sealed record TenantCreateRequest(string Key, string Name, string Plan, string DefaultLocale);

public sealed record BranchDto(Guid Id, string Code, string Name, string Address, bool IsActive);

public sealed record BranchRequest(string Code, string Name, string Address);

public sealed record LibraryPolicyDto(int MaxLoanDays, int MaxRenewals, decimal DailyFineAmount, int MaxActiveLoansPerMember);

public sealed record BookListItemDto(
    Guid Id,
    string Title,
    string Isbn,
    string Description,
    int? PublishedYear,
    string Language,
    IReadOnlyCollection<string> Authors,
    IReadOnlyCollection<string> Categories,
    string Publisher,
    IReadOnlyCollection<string> Tags,
    int TotalCopies,
    int AvailableCopies);

public sealed record BookDto(
    Guid Id,
    string Title,
    string Isbn,
    string Description,
    int? PublishedYear,
    string Language,
    IReadOnlyCollection<string> Authors,
    IReadOnlyCollection<string> Categories,
    string Publisher,
    IReadOnlyCollection<string> Tags,
    IReadOnlyCollection<BookCopyDto> Copies);

public sealed record BookCopyDto(Guid Id, Guid BookId, Guid BranchId, string BranchName, string Barcode, string QrCode, BookCopyStatus Status, string Location);

public sealed record CreateBookRequest(
    string Title,
    string Isbn,
    string Description,
    int? PublishedYear,
    string Language,
    string Publisher,
    IReadOnlyCollection<string> Authors,
    IReadOnlyCollection<string> Categories,
    IReadOnlyCollection<string> Tags);

public sealed record CreateCopyRequest(Guid BookId, Guid BranchId, string Barcode, string Location);

public sealed record MemberDto(Guid Id, Guid BranchId, string BranchName, string Code, string FullName, string Email, string Phone, MemberStatus Status, DateTimeOffset JoinedAt);

public sealed record MemberRequest(Guid BranchId, string Code, string FullName, string Email, string Phone);

public sealed record LoanDto(
    Guid Id,
    Guid MemberId,
    string MemberName,
    Guid BookCopyId,
    string Barcode,
    string BookTitle,
    Guid BranchId,
    string BranchName,
    DateTimeOffset LoanedAt,
    DateTimeOffset DueAt,
    DateTimeOffset? ReturnedAt,
    LoanStatus Status,
    int RenewalCount,
    decimal FineAmount);

public sealed record LoanRequest(Guid MemberId, Guid BranchId, string CopyBarcode);

public sealed record ReturnRequest(string CopyBarcode);

public sealed record RenewRequest(Guid LoanId);

public sealed record HoldDto(Guid Id, Guid BookId, string BookTitle, Guid MemberId, string MemberName, Guid BranchId, HoldStatus Status, DateTimeOffset RequestedAt, DateTimeOffset? ExpiresAt);

public sealed record HoldRequestPayload(Guid BookId, Guid MemberId, Guid BranchId);

public sealed record NotificationDto(Guid Id, Guid? BranchId, string Channel, string MessageKey, IReadOnlyDictionary<string, string> Variables, NotificationStatus Status, DateTimeOffset CreatedAt);

public sealed record AuditLogDto(Guid Id, Guid? BranchId, string ActorName, string Action, string EntityName, Guid? EntityId, string Summary, DateTimeOffset CreatedAt);

public sealed record DashboardSummaryDto(
    int BookCount,
    int CopyCount,
    int AvailableCopies,
    int LoanedCopies,
    int OverdueLoans,
    int MemberCount,
    decimal OpenFineAmount,
    IReadOnlyCollection<BranchKpiDto> Branches,
    IReadOnlyCollection<RecentActivityDto> RecentActivities,
    IReadOnlyCollection<PopularBookDto> PopularBooks);

public sealed record BranchKpiDto(Guid BranchId, string BranchName, int Copies, int ActiveLoans, int OverdueLoans);

public sealed record RecentActivityDto(string Action, string Summary, DateTimeOffset CreatedAt);

public sealed record PopularBookDto(Guid BookId, string Title, int LoanCount);

public sealed record AiSearchRequest(string Query);

public sealed record AiSearchResultDto(Guid BookId, string Title, string Isbn, int AvailableCopies, decimal Score, string Explanation);

public sealed record AiSearchResponse(string Query, bool UsedFallback, IReadOnlyCollection<AiSearchResultDto> Results, IReadOnlyCollection<string> Guardrails);

public sealed record AiChatRequest(string Message);

public sealed record AiChatResponse(string Answer, IReadOnlyCollection<string> Citations, bool UsedFallback);
