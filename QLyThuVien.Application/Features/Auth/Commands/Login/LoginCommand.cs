using MediatR;
using QLyThuVien.Application.Features.Auth.Common;

namespace QLyThuVien.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(LoginRequest Request) : IRequest<LoginResponse>;
