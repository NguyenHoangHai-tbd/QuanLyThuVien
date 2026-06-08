using MediatR;
using QLyThuVien.Application.Features.Holds.Common;

namespace QLyThuVien.Application.Features.Holds.Commands.Cancel;

public sealed record CancelHoldCommand(Guid Id) : IRequest<HoldDto>;

