using MediatR;
using QLyThuVien.Application.Features.Ai.Common;

namespace QLyThuVien.Application.Features.Ai.Commands.AiSearch;

public sealed record AiSearchCommand(AiSearchRequest Request) : IRequest<AiSearchResponse>;
