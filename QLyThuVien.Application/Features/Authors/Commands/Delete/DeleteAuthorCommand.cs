using MediatR;

namespace QLyThuVien.Application.Features.Authors.Commands.Delete;

public sealed record DeleteAuthorCommand(Guid Id) : IRequest;
