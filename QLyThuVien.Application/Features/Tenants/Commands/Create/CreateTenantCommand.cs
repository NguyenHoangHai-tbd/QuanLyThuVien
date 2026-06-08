using MediatR;
using QLyThuVien.Application.Features.Tenants.Common;

namespace QLyThuVien.Application.Features.Tenants.Commands.Create;

public sealed record CreateTenantCommand(TenantCreateRequest Request) : IRequest<TenantDto>;

