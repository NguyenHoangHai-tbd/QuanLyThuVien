using MediatR;
using QLyThuVien.Application.Features.Members.Common;

namespace QLyThuVien.Application.Features.Members.Commands.Update;

public sealed record UpdateMemberCommand(Guid Id, UpdateMemberRequest Request) : IRequest<MemberDto>;
