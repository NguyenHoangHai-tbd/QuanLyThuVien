using MediatR;
using QLyThuVien.Application.Features.System.Common;

namespace QLyThuVien.Application.Features.System.Queries;

public sealed record GetDatabaseConnectionStatusQuery : IRequest<DatabaseConnectionStatusDto>;
