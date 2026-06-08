using MediatR;

namespace QLyThuVien.Application.Features.Branches.Commands.Delete;

public sealed record DeleteBranchCommand(Guid Id) : IRequest;

