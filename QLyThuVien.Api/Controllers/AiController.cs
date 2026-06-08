using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLyThuVien.Application.Features.Ai.Commands.AiChat;
using QLyThuVien.Application.Features.Ai.Commands.AiSearch;
using QLyThuVien.Application.Features.Ai.Common;

namespace QLyThuVien.Api.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public sealed class AiController : ControllerBase
{
    private readonly ISender _sender;

    public AiController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("search")]
    public Task<AiSearchResponse> Search(AiSearchRequest request, CancellationToken cancellationToken)
        => _sender.Send(new AiSearchCommand(request), cancellationToken);

    [HttpPost("chat")]
    public Task<AiChatResponse> Chat(AiChatRequest request, CancellationToken cancellationToken)
        => _sender.Send(new AiChatCommand(request), cancellationToken);
}
