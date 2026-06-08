using MediatR;
using QLyThuVien.Application.Features.Branches.Common;

namespace QLyThuVien.Application.Features.Branches.Commands.Update;

public sealed record UpdateBranchCommand(Guid Id, BranchUpdateRequest Request) : IRequest<BranchDto>;

