using MediatR;

namespace QLyThuVien.Application.Features.Tenants.Commands.Delete;

public sealed record DeleteTenantCommand(Guid Id) : IRequest;

