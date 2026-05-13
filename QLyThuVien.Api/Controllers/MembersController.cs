using Microsoft.AspNetCore.Mvc;
using QLyThuVien.Application.Dtos;
using QLyThuVien.Application.Services;

namespace QLyThuVien.Api.Controllers;

[ApiController]
[Route("api/members")]
public sealed class MembersController : ControllerBase
{
    private readonly MemberService _memberService;

    public MembersController(MemberService memberService)
    {
        _memberService = memberService;
    }

    [HttpGet]
    public Task<IReadOnlyCollection<MemberDto>> SearchMembers([FromQuery] string? search, [FromQuery] Guid? branchId, CancellationToken cancellationToken)
        => _memberService.SearchMembersAsync(search, branchId, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<MemberDto> GetMember(Guid id, CancellationToken cancellationToken)
        => _memberService.GetMemberAsync(id, cancellationToken);

    [HttpPost]
    public Task<MemberDto> CreateMember(MemberRequest request, CancellationToken cancellationToken)
        => _memberService.CreateMemberAsync(request, cancellationToken);
}
