using QLyThuVien.Application.Abstractions;
using QLyThuVien.Application.Common;
using QLyThuVien.Application.Dtos;
using QLyThuVien.Domain.Entities;
using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Services;

public sealed class MemberService : ApplicationServiceBase
{
    public MemberService(ILibraryRepository repository, ICurrentUserContext currentUser, IClock clock)
        : base(repository, currentUser, clock)
    {
    }

    public Task<IReadOnlyCollection<MemberDto>> SearchMembersAsync(string? search, Guid? branchId, CancellationToken cancellationToken = default)
    {
        var query = Clean(search);
        var members = BranchScope(Repository.Members);

        if (branchId.HasValue)
        {
            EnsureBranchAccess(branchId.Value);
            members = members.Where(x => x.BranchId == branchId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            members = members.Where(x =>
                HasText(x.Code, query) ||
                HasText(x.FullName, query) ||
                HasText(x.Email, query) ||
                HasText(x.Phone, query));
        }

        var result = members.OrderBy(x => x.FullName).Select(MapMember).ToArray();
        return Task.FromResult<IReadOnlyCollection<MemberDto>>(result);
    }

    public async Task<MemberDto> CreateMemberAsync(MemberRequest request, CancellationToken cancellationToken = default)
    {
        var branch = GetBranch(request.BranchId);
        var code = Clean(request.Code).ToUpperInvariant();
        var fullName = Clean(request.FullName);

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(fullName))
        {
            throw AppException.BadRequest("Member code and full name are required.");
        }

        if (TenantScope(Repository.Members).Any(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
        {
            throw AppException.Conflict("Member code already exists.");
        }

        var member = new MemberProfile
        {
            TenantId = TenantId,
            BranchId = branch.Id,
            Code = code,
            FullName = fullName,
            Email = Clean(request.Email),
            Phone = Clean(request.Phone),
            Status = MemberStatus.Active,
            JoinedAt = Clock.UtcNow,
            CreatedAt = Clock.UtcNow,
            CreatedBy = CurrentUser.Email
        };

        Repository.AddMember(member);
        AddAudit("member.created", "MemberProfile", member.Id, $"Created member {member.FullName}", branch.Id);
        await Repository.SaveChangesAsync(cancellationToken);

        return MapMember(member);
    }

    public Task<MemberDto> GetMemberAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var member = BranchScope(Repository.Members).FirstOrDefault(x => x.Id == id);
        if (member is null)
        {
            throw AppException.NotFound("Member not found.");
        }

        return Task.FromResult(MapMember(member));
    }

    private MemberDto MapMember(MemberProfile member)
    {
        var branch = Repository.Branches.FirstOrDefault(x => x.Id == member.BranchId);
        return new MemberDto(
            member.Id,
            member.BranchId,
            branch?.Name ?? string.Empty,
            member.Code,
            member.FullName,
            member.Email,
            member.Phone,
            member.Status,
            member.JoinedAt);
    }
}
