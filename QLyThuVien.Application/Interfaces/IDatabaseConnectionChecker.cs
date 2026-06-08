using QLyThuVien.Application.Features.System.Common;

namespace QLyThuVien.Application.Interfaces;

public interface IDatabaseConnectionChecker
{
    Task EnsureCreatedAsync(CancellationToken cancellationToken = default);

    Task<DatabaseConnectionStatusDto> CheckAsync(CancellationToken cancellationToken = default);
}
