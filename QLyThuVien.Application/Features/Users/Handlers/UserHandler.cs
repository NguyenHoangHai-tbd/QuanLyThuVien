using MediatR;
using QLyThuVien.Application.Common;
using QLyThuVien.Application.Interfaces;
using QLyThuVien.Application.Features.Users.Common;
using QLyThuVien.Application.Features.Users.Commands.Create;
using QLyThuVien.Application.Features.Users.Commands.Delete;
using QLyThuVien.Application.Features.Users.Commands.Update;
using QLyThuVien.Application.Features.Users.Queries;
using QLyThuVien.Domain.Entities;
using QLyThuVien.Domain.Enums;

namespace QLyThuVien.Application.Features.Users.Handlers;

public sealed class UserHandler :
    ApplicationRequestHandlerBase,
    IRequestHandler<SearchUsersQuery, IReadOnlyCollection<UserAccountDto>>,
    IRequestHandler<GetUserQuery, UserAccountDto>,
    IRequestHandler<CreateUserCommand, UserAccountDto>,
    IRequestHandler<UpdateUserCommand, UserAccountDto>,
    IRequestHandler<DeleteUserCommand>
{
    private readonly IPasswordHasher _passwordHasher;

    public UserHandler(
        ILibraryRepository repository,
        ICurrentUserContext currentUser,
        IClock clock,
        IPasswordHasher passwordHasher)
        : base(repository, currentUser, clock)
    {
        _passwordHasher = passwordHasher;
    }

    public Task<IReadOnlyCollection<UserAccountDto>> Handle(SearchUsersQuery query, CancellationToken cancellationToken)
    {
        EnsureCanManageUsers();
        var search = Clean(query.Search);
        var users = TenantScope(Repository.Users);

        if (!string.IsNullOrWhiteSpace(search))
        {
            users = users.Where(x =>
                HasText(x.FullName, search) ||
                HasText(x.Email, search) ||
                x.Role.ToString().Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (query.BranchId.HasValue)
        {
            EnsureBranchAccess(query.BranchId.Value);
            users = users.Where(x =>
                x.Role is UserRole.SuperAdmin or UserRole.TenantAdmin ||
                x.BranchIds.Contains(query.BranchId.Value));
        }

        var result = users
            .OrderBy(x => x.FullName)
            .Select(MapUser)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<UserAccountDto>>(result);
    }

    public Task<UserAccountDto> Handle(GetUserQuery query, CancellationToken cancellationToken)
    {
        EnsureCanManageUsers();
        var user = TenantScope(Repository.Users).FirstOrDefault(x => x.Id == query.Id);
        if (user is null)
        {
            throw AppException.NotFound("User not found.");
        }

        return Task.FromResult(MapUser(user));
    }

    public async Task<UserAccountDto> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        EnsureCanManageUsers();
        ValidateUserInput(request.FullName, request.Email, request.Role, request.BranchIds);

        var email = Clean(request.Email).ToLowerInvariant();
        if (TenantScope(Repository.Users).Any(x => x.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
        {
            throw AppException.Conflict("Email already exists in this tenant.");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            throw AppException.BadRequest("Password must have at least 6 characters.");
        }

        var user = new UserAccount
        {
            TenantId = TenantId,
            FullName = Clean(request.FullName),
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = request.Role,
            BranchIds = NormalizeBranchIds(request.Role, request.BranchIds),
            Locale = string.IsNullOrWhiteSpace(request.Locale) ? CurrentUser.Locale : request.Locale.Trim(),
            IsActive = request.IsActive,
            CreatedAt = Clock.UtcNow,
            CreatedBy = CurrentUser.Email
        };

        Repository.AddUser(user);
        AddAudit("identity.user.created", "UserAccount", user.Id, $"Created user {user.Email}");
        await Repository.SaveChangesAsync(cancellationToken);

        return MapUser(user);
    }

    public async Task<UserAccountDto> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        EnsureCanManageUsers();
        ValidateUserInput(request.FullName, request.Email, request.Role, request.BranchIds);

        var user = TenantScope(Repository.Users).FirstOrDefault(x => x.Id == command.Id);
        if (user is null)
        {
            throw AppException.NotFound("User not found.");
        }

        if (user.Id == CurrentUser.UserId && !request.IsActive)
        {
            throw AppException.BadRequest("You cannot disable your own account.");
        }

        var email = Clean(request.Email).ToLowerInvariant();
        if (TenantScope(Repository.Users).Any(x => x.Id != command.Id && x.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
        {
            throw AppException.Conflict("Email already exists in this tenant.");
        }

        user.FullName = Clean(request.FullName);
        user.Email = email;
        user.Role = request.Role;
        user.BranchIds = NormalizeBranchIds(request.Role, request.BranchIds);
        user.Locale = string.IsNullOrWhiteSpace(request.Locale) ? CurrentUser.Locale : request.Locale.Trim();
        user.IsActive = request.IsActive;
        user.UpdatedAt = Clock.UtcNow;
        user.UpdatedBy = CurrentUser.Email;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            if (request.Password.Length < 6)
            {
                throw AppException.BadRequest("Password must have at least 6 characters.");
            }

            user.PasswordHash = _passwordHasher.Hash(request.Password);
        }

        AddAudit("identity.user.updated", "UserAccount", user.Id, $"Updated user {user.Email}");
        await Repository.SaveChangesAsync(cancellationToken);

        return MapUser(user);
    }

    public async Task Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        EnsureCanManageUsers();
        var user = TenantScope(Repository.Users).FirstOrDefault(x => x.Id == command.Id);
        if (user is null)
        {
            throw AppException.NotFound("User not found.");
        }

        if (user.Id == CurrentUser.UserId)
        {
            throw AppException.BadRequest("You cannot delete your own account.");
        }

        user.IsDeleted = true;
        user.IsActive = false;
        user.UpdatedAt = Clock.UtcNow;
        user.UpdatedBy = CurrentUser.Email;

        AddAudit("identity.user.deleted", "UserAccount", user.Id, $"Deleted user {user.Email}");
        await Repository.SaveChangesAsync(cancellationToken);
    }

    private void EnsureCanManageUsers()
    {
        EnsureAuthenticated();
        if (CurrentUser.Role is not (UserRole.SuperAdmin or UserRole.TenantAdmin))
        {
            throw AppException.Forbidden("Only admins can manage users.");
        }
    }

    private void ValidateUserInput(string fullName, string email, UserRole role, IReadOnlyCollection<Guid> branchIds)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
        {
            throw AppException.BadRequest("Full name and email are required.");
        }

        if (!email.Contains('@', StringComparison.Ordinal))
        {
            throw AppException.BadRequest("Email format is invalid.");
        }

        if (role is not (UserRole.SuperAdmin or UserRole.TenantAdmin) && branchIds.Count == 0)
        {
            throw AppException.BadRequest("Branch scope is required for non-admin users.");
        }

        foreach (var branchId in branchIds)
        {
            GetBranch(branchId);
        }
    }

    private static List<Guid> NormalizeBranchIds(UserRole role, IReadOnlyCollection<Guid> branchIds)
    {
        if (role is UserRole.SuperAdmin or UserRole.TenantAdmin)
        {
            return [];
        }

        return branchIds.Distinct().ToList();
    }

    private UserAccountDto MapUser(UserAccount user)
    {
        var branchNames = user.BranchIds
            .Select(id => Repository.Branches.FirstOrDefault(x => x.Id == id && x.TenantId == user.TenantId)?.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .ToArray();

        return new UserAccountDto(
            user.Id,
            user.FullName,
            user.Email,
            user.Role,
            user.BranchIds,
            branchNames,
            user.Locale,
            user.IsActive,
            user.CreatedAt);
    }
}
