namespace QLyThuVien.Application.Features.Ai.Common;

public sealed record AiSearchResponse(string Query, bool UsedFallback, IReadOnlyCollection<AiSearchResultDto> Results, IReadOnlyCollection<string> Guardrails);
