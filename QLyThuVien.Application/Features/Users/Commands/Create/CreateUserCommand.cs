using MediatR;
using QLyThuVien.Application.Features.Users.Common;

namespace QLyThuVien.Application.Features.Users.Commands.Create;

public sealed record CreateUserCommand(CreateUserRequest Request) : IRequest<UserAccountDto>;
