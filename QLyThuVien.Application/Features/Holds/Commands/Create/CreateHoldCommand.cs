using MediatR;
using QLyThuVien.Application.Features.Holds.Common;

namespace QLyThuVien.Application.Features.Holds.Commands.Create;

public sealed record CreateHoldCommand(HoldRequestPayload Request) : IRequest<HoldDto>;

