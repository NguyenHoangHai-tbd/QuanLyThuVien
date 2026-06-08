using MediatR;
using QLyThuVien.Application.Features.Branches.Common;

namespace QLyThuVien.Application.Features.Branches.Commands.Create;

public sealed record CreateBranchCommand(BranchRequest Request) : IRequest<BranchDto>;

