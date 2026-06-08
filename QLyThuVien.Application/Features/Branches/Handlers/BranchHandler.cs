using MediatR;
using QLyThuVien.Application.Common;
using QLyThuVien.Application.Features.Branches.Commands.Create;
using QLyThuVien.Application.Features.Branches.Commands.Delete;
using QLyThuVien.Application.Features.Branches.Commands.Update;
using QLyThuVien.Application.Features.Branches.Common;
using QLyThuVien.Application.Features.Branches.Queries;
using QLyThuVien.Application.Interfaces;
using QLyThuVien.Domain.Entities;
using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Features.Branches.Handlers;

public sealed class BranchHandler :
    ApplicationRequestHandlerBase,
    IRequestHandler<GetBranchesQuery, IReadOnlyCollection<BranchDto>>,
    IRequestHandler<CreateBranchCommand, BranchDto>,
    IRequestHandler<UpdateBranchCommand, BranchDto>,
    IRequestHandler<DeleteBranchCommand>
{
    public BranchHandler(ILibraryRepository repository, ICurrentUserContext currentUser, IClock clock)
        : base(repository, currentUser, clock)
    {
    }

    public Task<IReadOnlyCollection<BranchDto>> Handle(GetBranchesQuery query, CancellationToken cancellationToken)
    {
        var branches = TenantScope(Repository.Branches)
            .Where(x => CurrentUser.CanAccessBranch(x.Id))
            .OrderBy(x => x.Name)
            .Select(MapBranch)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<BranchDto>>(branches);
    }

    public async Task<BranchDto> Handle(CreateBranchCommand command, CancellationToken cancellationToken)
    {
        EnsureTenantAdmin("Only tenant admins can create branches.");

        var request = command.Request;
        var code = Clean(request.Code).ToUpperInvariant();
        var name = Clean(request.Name);

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            throw AppException.BadRequest("Branch code and name are required.");
        }

        if (TenantScope(Repository.Branches).Any(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
        {
            throw AppException.Conflict("Branch code already exists.");
        }

        var branch = new Branch
        {
            TenantId = TenantId,
            Code = code,
            Name = name,
            Address = Clean(request.Address),
            CreatedAt = Clock.UtcNow,
            CreatedBy = CurrentUser.Email
        };

        Repository.AddBranch(branch);
        AddAudit("branch.created", "Branch", branch.Id, $"Created branch {branch.Name}", branch.Id);
        await Repository.SaveChangesAsync(cancellationToken);

        return MapBranch(branch);
    }

    public async Task<BranchDto> Handle(UpdateBranchCommand command, CancellationToken cancellationToken)
    {
        EnsureTenantAdmin("Only tenant admins can update branches.");

        var request = command.Request;
        var branch = GetBranch(command.Id);
        var code = Clean(request.Code).ToUpperInvariant();
        var name = Clean(request.Name);

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            throw AppException.BadRequest("Branch code and name are required.");
        }

        if (TenantScope(Repository.Branches).Any(x =>
                x.Id != command.Id &&
                x.Code.Equals(code, StringComparison.OrdinalIgnoreCase)))
        {
            throw AppException.Conflict("Branch code already exists.");
        }

        branch.Code = code;
        branch.Name = name;
        branch.Address = Clean(request.Address);
        branch.IsActive = request.IsActive;
        branch.UpdatedAt = Clock.UtcNow;
        branch.UpdatedBy = CurrentUser.Email;

        AddAudit("branch.updated", "Branch", branch.Id, $"Updated branch {branch.Name}", branch.Id);
        await Repository.SaveChangesAsync(cancellationToken);

        return MapBranch(branch);
    }

    public async Task Handle(DeleteBranchCommand command, CancellationToken cancellationToken)
    {
        EnsureTenantAdmin("Only tenant admins can delete branches.");

        var branch = GetBranch(command.Id);
        var hasActiveLoans = TenantScope(Repository.Loans).Any(x =>
            x.BranchId == branch.Id &&
            x.Status is LoanStatus.Active or LoanStatus.Overdue);

        if (hasActiveLoans)
        {
            throw AppException.BadRequest("Cannot delete a branch with active loans.");
        }

        branch.IsDeleted = true;
        branch.IsActive = false;
        branch.UpdatedAt = Clock.UtcNow;
        branch.UpdatedBy = CurrentUser.Email;

        AddAudit("branch.deleted", "Branch", branch.Id, $"Deleted branch {branch.Name}", branch.Id);
        await Repository.SaveChangesAsync(cancellationToken);
    }

    private void EnsureTenantAdmin(string message)
    {
        EnsureAuthenticated();

        if (CurrentUser.Role is not (UserRole.SuperAdmin or UserRole.TenantAdmin))
        {
            throw AppException.Forbidden(message);
        }
    }

    private static BranchDto MapBranch(Branch branch)
        => new(branch.Id, branch.Code, branch.Name, branch.Address, branch.IsActive);
}
