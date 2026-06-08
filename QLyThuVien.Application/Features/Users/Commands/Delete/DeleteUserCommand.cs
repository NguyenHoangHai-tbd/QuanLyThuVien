using MediatR;

namespace QLyThuVien.Application.Features.Users.Commands.Delete;

public sealed record DeleteUserCommand(Guid Id) : IRequest;
