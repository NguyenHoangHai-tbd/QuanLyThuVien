using MediatR;
using QLyThuVien.Application.Features.Users.Common;

namespace QLyThuVien.Application.Features.Users.Commands.Update;

public sealed record UpdateUserCommand(Guid Id, UpdateUserRequest Request) : IRequest<UserAccountDto>;
