using MediatR;
using QLyThuVien.Application.Features.Auth.Common;

namespace QLyThuVien.Application.Features.Auth.Commands.Logout;

public sealed record LogoutCommand : IRequest<LogoutResponse>;
