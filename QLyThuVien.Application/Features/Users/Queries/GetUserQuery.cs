using MediatR;
using QLyThuVien.Application.Features.Users.Common;

namespace QLyThuVien.Application.Features.Users.Queries;

public sealed record GetUserQuery(Guid Id) : IRequest<UserAccountDto>;
