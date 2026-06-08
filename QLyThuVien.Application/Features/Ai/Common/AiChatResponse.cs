namespace QLyThuVien.Application.Features.Ai.Common;

public sealed record AiChatResponse(string Answer, IReadOnlyCollection<string> Citations, bool UsedFallback);
