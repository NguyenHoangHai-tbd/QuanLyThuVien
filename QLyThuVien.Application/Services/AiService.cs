using QLyThuVien.Application.Abstractions;
using QLyThuVien.Application.Common;
using QLyThuVien.Application.Dtos;
using QLyThuVien.Domain.Entities;
using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Services;

public sealed class AiService : ApplicationServiceBase
{
    public AiService(ILibraryRepository repository, ICurrentUserContext currentUser, IClock clock)
        : base(repository, currentUser, clock)
    {
    }

    public async Task<AiSearchResponse> SemanticSearchAsync(AiSearchRequest request, CancellationToken cancellationToken = default)
    {
        var query = Clean(request.Query);
        if (string.IsNullOrWhiteSpace(query))
        {
            throw AppException.BadRequest("Search query is required.");
        }

        var queryTerms = query
            .Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.ToLowerInvariant())
            .ToArray();

        var results = TenantScope(Repository.Books)
            .Select(book => ScoreBook(book, query, queryTerms))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Book.Title)
            .Take(8)
            .Select(x =>
            {
                var availableCopies = BranchScope(Repository.BookCopies).Count(copy =>
                    copy.BookId == x.Book.Id &&
                    copy.Status == BookCopyStatus.Available);

                return new AiSearchResultDto(
                    x.Book.Id,
                    x.Book.Title,
                    x.Book.Isbn,
                    availableCopies,
                    Math.Round(x.Score, 2),
                    x.Explanation);
            })
            .ToArray();

        Repository.AddAiUsageLog(new AiUsageLog
        {
            TenantId = TenantId,
            UserId = CurrentUser.UserId,
            Feature = "semantic-search",
            Prompt = query,
            ResultCount = results.Length,
            UsedFallback = true,
            CreatedAt = Clock.UtcNow,
            CreatedBy = CurrentUser.Email
        });
        AddAudit("ai.semantic_search", "AiUsageLog", null, $"AI search fallback used for query '{query}'");
        await Repository.SaveChangesAsync(cancellationToken);

        var guardrails = new[]
        {
            "tenant_scope_enforced",
            "branch_scope_enforced",
            "no_cross_tenant_data",
            "fallback_keyword_semantic_scoring"
        };

        return new AiSearchResponse(query, true, results, guardrails);
    }

    public async Task<AiChatResponse> ChatAsync(AiChatRequest request, CancellationToken cancellationToken = default)
    {
        var message = Clean(request.Message);
        if (string.IsNullOrWhiteSpace(message))
        {
            throw AppException.BadRequest("Message is required.");
        }

        var branchCount = TenantScope(Repository.Branches).Count(x => CurrentUser.CanAccessBranch(x.Id));
        var bookCount = TenantScope(Repository.Books).Count();
        var activeLoans = BranchScope(Repository.Loans).Count(x => x.Status is LoanStatus.Active or LoanStatus.Overdue);
        var overdueLoans = BranchScope(Repository.Loans).Count(x => x.Status == LoanStatus.Overdue || x.DueAt < Clock.UtcNow);

        var answer =
            $"Theo pham vi tenant {CurrentUser.TenantName}, he thong dang co {branchCount} chi nhanh ban duoc phep xem, " +
            $"{bookCount} dau sach, {activeLoans} luot muon dang mo va {overdueLoans} luot qua han. " +
            "Day la cau tra loi fallback, chi dua tren du lieu noi bo da loc theo tenant/branch/role.";

        Repository.AddAiUsageLog(new AiUsageLog
        {
            TenantId = TenantId,
            UserId = CurrentUser.UserId,
            Feature = "tenant-chat",
            Prompt = message,
            ResultCount = 1,
            UsedFallback = true,
            CreatedAt = Clock.UtcNow,
            CreatedBy = CurrentUser.Email
        });
        AddAudit("ai.chat", "AiUsageLog", null, $"AI chat fallback answered message '{message}'");
        await Repository.SaveChangesAsync(cancellationToken);

        return new AiChatResponse(
            answer,
            ["DashboardSummary", "Loans", "Books"],
            true);
    }

    private (Book Book, decimal Score, string Explanation) ScoreBook(Book book, string query, IReadOnlyCollection<string> terms)
    {
        decimal score = 0;
        var reasons = new List<string>();

        if (book.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            score += 5;
            reasons.Add("matched_title");
        }

        if (book.Isbn.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            score += 4;
            reasons.Add("matched_isbn");
        }

        if (book.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            score += 2;
            reasons.Add("matched_description");
        }

        foreach (var term in terms)
        {
            if (book.Tags.Any(tag => tag.Contains(term, StringComparison.OrdinalIgnoreCase)))
            {
                score += 1.5m;
                reasons.Add($"tag:{term}");
            }

            var authorMatch = Repository.Authors
                .Where(x => book.AuthorIds.Contains(x.Id))
                .Any(x => x.Name.Contains(term, StringComparison.OrdinalIgnoreCase));

            if (authorMatch)
            {
                score += 1;
                reasons.Add($"author:{term}");
            }

            var categoryMatch = Repository.Categories
                .Where(x => book.CategoryIds.Contains(x.Id))
                .Any(x => x.Name.Contains(term, StringComparison.OrdinalIgnoreCase));

            if (categoryMatch)
            {
                score += 1;
                reasons.Add($"category:{term}");
            }
        }

        var availableCopies = BranchScope(Repository.BookCopies).Count(x => x.BookId == book.Id && x.Status == BookCopyStatus.Available);
        if (score > 0 && availableCopies > 0)
        {
            score += 0.5m;
            reasons.Add("available_copy");
        }

        return (book, score, reasons.Count == 0 ? "no_match" : string.Join(", ", reasons.Distinct()));
    }
}
