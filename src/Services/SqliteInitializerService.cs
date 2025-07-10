using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Data.Sqlite;
using Dapper;
using Kosync.Database;

namespace Kosync.Services;

public class SqliteOptions
{
    public static string SectionName = DatabaseProviders.SQLite;
    
    public required string ConnectionString { get; init; }
    
    public required string AdminPassword { get; init; }
}

public class SqliteInitializerService(IOptions<SqliteOptions> options) : IHostedService
{
    private readonly SqliteOptions _sqliteOptions = options.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await InitializeDatabaseAsync(cancellationToken);
        await CreateIndexesAsync(cancellationToken);
    }

    private async Task InitializeDatabaseAsync(CancellationToken cancellationToken)
    {
        using var connection = new SqliteConnection(_sqliteOptions.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS Users (
                Id TEXT PRIMARY KEY,
                Username TEXT UNIQUE NOT NULL,
                PasswordHash TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                IsAdministrator INTEGER NOT NULL DEFAULT 0,
                Documents TEXT NOT NULL DEFAULT '{}'
            );
            """);
    }

    private async Task CreateIndexesAsync(CancellationToken cancellationToken)
    {
        using var connection = new SqliteConnection(_sqliteOptions.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await connection.ExecuteAsync("""
            CREATE INDEX IF NOT EXISTS idx_users_username ON Users(Username);
            CREATE INDEX IF NOT EXISTS idx_users_active ON Users(IsActive);
            CREATE INDEX IF NOT EXISTS idx_users_admin ON Users(IsAdministrator);
            """);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}