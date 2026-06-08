using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QLyThuVien.Application.Interfaces;
using QLyThuVien.Application.Features.System.Common;

namespace QLyThuVien.Infrastructure.Persistence;

public sealed class SqlServerDatabaseConnectionChecker : IDatabaseConnectionChecker
{
    private readonly string _connectionString;
    private readonly ILogger<SqlServerDatabaseConnectionChecker> _logger;
    private readonly string _provider;

    public SqlServerDatabaseConnectionChecker(IConfiguration configuration, ILogger<SqlServerDatabaseConnectionChecker> logger)
    {
        _logger = logger;
        _provider = configuration["Database:Provider"] ?? "SqlServer";
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }

    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            _logger.LogWarning("DefaultConnection is not configured.");
            return;
        }

        var builder = new SqlConnectionStringBuilder(_connectionString);
        var databaseName = string.IsNullOrWhiteSpace(builder.InitialCatalog)
            ? "QLyThuVienDb"
            : builder.InitialCatalog;

        var masterBuilder = new SqlConnectionStringBuilder(builder.ConnectionString)
        {
            InitialCatalog = "master"
        };

        await using (var masterConnection = new SqlConnection(masterBuilder.ConnectionString))
        {
            await masterConnection.OpenAsync(cancellationToken);
            await using var createDatabaseCommand = masterConnection.CreateCommand();
            createDatabaseCommand.CommandText =
                $"""
                IF DB_ID(N'{EscapeSqlLiteral(databaseName)}') IS NULL
                BEGIN
                    CREATE DATABASE [{EscapeIdentifier(databaseName)}];
                END
                """;
            await createDatabaseCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        builder.InitialCatalog = databaseName;
        await using var appConnection = new SqlConnection(builder.ConnectionString);
        await appConnection.OpenAsync(cancellationToken);
        await using var createInfoTableCommand = appConnection.CreateCommand();
        createInfoTableCommand.CommandText =
            """
            IF OBJECT_ID(N'dbo.__AppInfo', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.__AppInfo
                (
                    Id int NOT NULL CONSTRAINT PK___AppInfo PRIMARY KEY,
                    Name nvarchar(100) NOT NULL,
                    CreatedAt datetimeoffset NOT NULL
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM dbo.__AppInfo WHERE Id = 1)
            BEGIN
                INSERT INTO dbo.__AppInfo (Id, Name, CreatedAt)
                VALUES (1, N'QLyThuVien', SYSDATETIMEOFFSET());
            END;
            """;
        await createInfoTableCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<DatabaseConnectionStatusDto> CheckAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return new DatabaseConnectionStatusDto(_provider, string.Empty, string.Empty, false, "DefaultConnection is not configured.");
        }

        try
        {
            await EnsureCreatedAsync(cancellationToken);
            var builder = new SqlConnectionStringBuilder(_connectionString);
            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            return new DatabaseConnectionStatusDto(
                _provider,
                builder.DataSource,
                builder.InitialCatalog,
                true,
                "Connected to LocalDB successfully.");
        }
        catch (Exception exception)
        {
            var builder = new SqlConnectionStringBuilder(_connectionString);
            return new DatabaseConnectionStatusDto(
                _provider,
                builder.DataSource,
                builder.InitialCatalog,
                false,
                exception.Message);
        }
    }

    private static string EscapeIdentifier(string value) => value.Replace("]", "]]", StringComparison.Ordinal);

    private static string EscapeSqlLiteral(string value) => value.Replace("'", "''", StringComparison.Ordinal);
}

public sealed class DatabaseInitializerHostedService : IHostedService
{
    private readonly IDatabaseConnectionChecker _connectionChecker;
    private readonly ILogger<DatabaseInitializerHostedService> _logger;

    public DatabaseInitializerHostedService(
        IDatabaseConnectionChecker connectionChecker,
        ILogger<DatabaseInitializerHostedService> logger)
    {
        _connectionChecker = connectionChecker;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _connectionChecker.EnsureCreatedAsync(cancellationToken);
            _logger.LogInformation("Database initialization completed.");
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Database initialization failed: {exception.Message}");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
