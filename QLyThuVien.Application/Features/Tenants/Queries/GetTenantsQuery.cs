using MediatR;
using QLyThuVien.Application.Features.Tenants.Common;

namespace QLyThuVien.Application.Features.Tenants.Queries;

public sealed record GetTenantsQuery : IRequest<IReadOnlyCollection<TenantDto>>;

