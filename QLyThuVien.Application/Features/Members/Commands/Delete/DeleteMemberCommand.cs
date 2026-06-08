using MediatR;

namespace QLyThuVien.Application.Features.Members.Commands.Delete;

public sealed record DeleteMemberCommand(Guid Id) : IRequest;
