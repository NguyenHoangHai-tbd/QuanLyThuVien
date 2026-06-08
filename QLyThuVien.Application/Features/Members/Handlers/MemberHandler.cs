using MediatR;
using QLyThuVien.Application.Common;
using QLyThuVien.Application.Interfaces;
using QLyThuVien.Application.Features.Members.Common;
using QLyThuVien.Application.Features.Members.Commands.Create;
using QLyThuVien.Application.Features.Members.Commands.Delete;
using QLyThuVien.Application.Features.Members.Commands.Update;
using QLyThuVien.Application.Features.Members.Queries;
using QLyThuVien.Domain.Entities;
using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Features.Members.Handlers;

public sealed class MemberHandler :
    ApplicationRequestHandlerBase,
    IRequestHandler<SearchMembersQuery, IReadOnlyCollection<MemberDto>>,
    IRequestHandler<GetMemberQuery, MemberDto>,
    IRequestHandler<CreateMemberCommand, MemberDto>,
    IRequestHandler<UpdateMemberCommand, MemberDto>,
    IRequestHandler<DeleteMemberCommand>
{
    public MemberHandler(ILibraryRepository repository, ICurrentUserContext currentUser, IClock clock)
        : base(repository, currentUser, clock)
    {
    }

    public Task<IReadOnlyCollection<MemberDto>> Handle(SearchMembersQuery query, CancellationToken cancellationToken)
    {
        var search = Clean(query.Search);
        var members = BranchScope(Repository.Members);

        if (query.BranchId.HasValue)
        {
            EnsureBranchAccess(query.BranchId.Value);
            members = members.Where(x => x.BranchId == query.BranchId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            members = members.Where(x =>
                HasText(x.Code, search) ||
                HasText(x.FullName, search) ||
                HasText(x.Email, search) ||
                HasText(x.Phone, search));
        }

        var result = members.OrderBy(x => x.FullName).Select(MapMember).ToArray();
        return Task.FromResult<IReadOnlyCollection<MemberDto>>(result);
    }

    public Task<MemberDto> Handle(GetMemberQuery query, CancellationToken cancellationToken)
    {
        var member = BranchScope(Repository.Members).FirstOrDefault(x => x.Id == query.Id);
        if (member is null)
        {
            throw AppException.NotFound("Member not found.");
        }

        return Task.FromResult(MapMember(member));
    }

    public async Task<MemberDto> Handle(CreateMemberCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
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

    public async Task<MemberDto> Handle(UpdateMemberCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var member = BranchScope(Repository.Members).FirstOrDefault(x => x.Id == command.Id);
        if (member is null)
        {
            throw AppException.NotFound("Member not found.");
        }

        var branch = GetBranch(request.BranchId);
        var code = Clean(request.Code).ToUpperInvariant();
        var fullName = Clean(request.FullName);

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(fullName))
        {
            throw AppException.BadRequest("Member code and full name are required.");
        }

        if (TenantScope(Repository.Members).Any(x =>
                x.Id != command.Id &&
                x.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
        {
            throw AppException.Conflict("Member code already exists.");
        }

        member.BranchId = branch.Id;
        member.Code = code;
        member.FullName = fullName;
        member.Email = Clean(request.Email);
        member.Phone = Clean(request.Phone);
        member.Status = request.Status;
        member.UpdatedAt = Clock.UtcNow;
        member.UpdatedBy = CurrentUser.Email;

        AddAudit("member.updated", "MemberProfile", member.Id, $"Updated member {member.FullName}", branch.Id);
        await Repository.SaveChangesAsync(cancellationToken);

        return MapMember(member);
    }

    public async Task Handle(DeleteMemberCommand command, CancellationToken cancellationToken)
    {
        var member = BranchScope(Repository.Members).FirstOrDefault(x => x.Id == command.Id);
        if (member is null)
        {
            throw AppException.NotFound("Member not found.");
        }

        var hasActiveLoans = TenantScope(Repository.Loans).Any(x =>
            x.MemberId == member.Id &&
            x.Status is LoanStatus.Active or LoanStatus.Overdue);

        if (hasActiveLoans)
        {
            throw AppException.BadRequest("Cannot delete a member with active loans.");
        }

        member.IsDeleted = true;
        member.Status = MemberStatus.Expired;
        member.UpdatedAt = Clock.UtcNow;
        member.UpdatedBy = CurrentUser.Email;

        AddAudit("member.deleted", "MemberProfile", member.Id, $"Deleted member {member.FullName}", member.BranchId);
        await Repository.SaveChangesAsync(cancellationToken);
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
