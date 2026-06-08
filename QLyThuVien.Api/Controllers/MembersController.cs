using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using QLyThuVien.Application.Features.Members.Common;
using QLyThuVien.Application.Features.Members.Commands.Create;
using QLyThuVien.Application.Features.Members.Commands.Delete;
using QLyThuVien.Application.Features.Members.Commands.Update;
using QLyThuVien.Application.Features.Members.Queries;

namespace QLyThuVien.Api.Controllers;

[ApiController]
[Route("api/members")]
[Authorize(Roles = "SuperAdmin,TenantAdmin,Librarian")]
public sealed class MembersController : ControllerBase
{
    private readonly ISender _sender;

    public MembersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public Task<IReadOnlyCollection<MemberDto>> SearchMembers([FromQuery] string? search, [FromQuery] Guid? branchId, CancellationToken cancellationToken)
        => _sender.Send(new SearchMembersQuery(search, branchId), cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<MemberDto> GetMember(Guid id, CancellationToken cancellationToken)
        => _sender.Send(new GetMemberQuery(id), cancellationToken);

    [HttpPost]
    public Task<MemberDto> CreateMember(MemberRequest request, CancellationToken cancellationToken)
        => _sender.Send(new CreateMemberCommand(request), cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<MemberDto> UpdateMember(Guid id, UpdateMemberRequest request, CancellationToken cancellationToken)
        => _sender.Send(new UpdateMemberCommand(id, request), cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteMember(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteMemberCommand(id), cancellationToken);
        return NoContent();
    }
}
