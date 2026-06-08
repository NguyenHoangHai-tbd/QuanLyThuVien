using MediatR;
using QLyThuVien.Application.Common;
using QLyThuVien.Application.Features.Policies.Common;
using QLyThuVien.Application.Features.Policies.Queries;
using QLyThuVien.Application.Interfaces;
using QLyThuVien.Domain.Entities;

namespace QLyThuVien.Application.Features.Policies.Handlers;

public sealed class PolicyHandler : ApplicationRequestHandlerBase, IRequestHandler<GetCurrentPolicyQuery, LibraryPolicyDto>
{
    public PolicyHandler(ILibraryRepository repository, ICurrentUserContext currentUser, IClock clock)
        : base(repository, currentUser, clock)
    {
    }

    public Task<LibraryPolicyDto> Handle(GetCurrentPolicyQuery query, CancellationToken cancellationToken)
    {
        EnsureAuthenticated();

        var policy = Repository.Policies.FirstOrDefault(x => x.TenantId == TenantId && !x.IsDeleted)
            ?? new LibraryPolicy { TenantId = TenantId };

        return Task.FromResult(new LibraryPolicyDto(
            policy.MaxLoanDays,
            policy.MaxRenewals,
            policy.DailyFineAmount,
            policy.MaxActiveLoansPerMember));
    }
}
