using MediatR;
using QLyThuVien.Application.Features.Members.Common;

namespace QLyThuVien.Application.Features.Members.Commands.Create;

public sealed record CreateMemberCommand(MemberRequest Request) : IRequest<MemberDto>;
