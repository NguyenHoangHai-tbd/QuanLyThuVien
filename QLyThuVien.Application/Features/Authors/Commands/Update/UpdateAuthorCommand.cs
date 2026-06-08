using MediatR;
using QLyThuVien.Application.Features.Authors.Common;

namespace QLyThuVien.Application.Features.Authors.Commands.Update;

public sealed record UpdateAuthorCommand(Guid Id, AuthorRequest Request) : IRequest<AuthorDto>;
