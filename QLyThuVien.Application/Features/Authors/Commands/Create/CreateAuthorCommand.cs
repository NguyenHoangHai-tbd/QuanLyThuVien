using MediatR;
using QLyThuVien.Application.Features.Authors.Common;

namespace QLyThuVien.Application.Features.Authors.Commands.Create;

public sealed record CreateAuthorCommand(AuthorRequest Request) : IRequest<AuthorDto>;
