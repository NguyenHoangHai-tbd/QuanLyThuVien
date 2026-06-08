namespace QLyThuVien.Application.Features.System.Common;

public sealed record DatabaseConnectionStatusDto(
    string Provider,
    string DataSource,
    string Database,
    bool CanConnect,
    string Message);
