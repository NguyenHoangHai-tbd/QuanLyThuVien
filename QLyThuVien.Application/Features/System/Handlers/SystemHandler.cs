using MediatR;
using QLyThuVien.Application.Interfaces;
using QLyThuVien.Application.Features.System.Common;
using QLyThuVien.Application.Features.System.Queries;

namespace QLyThuVien.Application.Features.System.Handlers;

public sealed class SystemHandler : IRequestHandler<GetDatabaseConnectionStatusQuery, DatabaseConnectionStatusDto>
{
    private readonly IDatabaseConnectionChecker _databaseConnectionChecker;

    public SystemHandler(IDatabaseConnectionChecker databaseConnectionChecker)
    {
        _databaseConnectionChecker = databaseConnectionChecker;
    }

    public Task<DatabaseConnectionStatusDto> Handle(GetDatabaseConnectionStatusQuery query, CancellationToken cancellationToken)
        => _databaseConnectionChecker.CheckAsync(cancellationToken);
}
