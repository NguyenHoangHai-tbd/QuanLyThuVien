using MediatR;
using QLyThuVien.Application.Features.Ai.Common;

namespace QLyThuVien.Application.Features.Ai.Commands.AiChat;

public sealed record AiChatCommand(AiChatRequest Request) : IRequest<AiChatResponse>;
