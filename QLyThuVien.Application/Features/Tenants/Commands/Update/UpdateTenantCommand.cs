using MediatR;
using QLyThuVien.Application.Features.Tenants.Common;

namespace QLyThuVien.Application.Features.Tenants.Commands.Update;

public sealed record UpdateTenantCommand(Guid Id, TenantUpdateRequest Request) : IRequest<TenantDto>;

