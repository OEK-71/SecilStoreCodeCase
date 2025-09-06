using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SecilStoreCodeCase
{
    public class SqlConfigStore : IConfigStore
    {
        private readonly string _connectionString;
        public SqlConfigStore(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }
        private IDbConnection Create() => new SqlConnection(_connectionString);
        private async Task<IDbConnection> OpenForAppAsync(string app, CancellationToken ct)
        {
            var conn = Create();
            await ((SqlConnection)conn).OpenAsync(ct);
            // Bu connection için oturum bağlamına uygulama adını yaz
            await conn.ExecuteAsync(new CommandDefinition(
                "EXEC sys.sp_set_session_context @key=N'AppName', @value=@app;",
                new { app }, cancellationToken: ct));
            return conn;
        }

        public async Task<IReadOnlyList<ConfigurationItem>> GetActiveAsync(string applicationName,CancellationToken cancellationToken = default)
        {
            const string sql = @"
            SELECT Id,ApplicationName,Name,Type,Value,IsActive,UpdatedAt AS UpdatedAtUtc 
            FROM dbo.Configurations WHERE IsActive = 1";

            using var conn = await OpenForAppAsync(applicationName, cancellationToken);
            var rows = await conn.QueryAsync<ConfigurationItem>(new CommandDefinition(sql, new { app = applicationName }, cancellationToken: cancellationToken));
            return rows.ToList();
        }

        public async Task<IReadOnlyList<ConfigurationItem>> GetActiveChangedSinceAsync
            (string applicationName, DateTime sinceUtc, CancellationToken cancellationToken=default)
        {
            const string sql = @"SELECT Id,ApplicationName,Name,Type,Value,IsActive,UpdatedAt AS UpdatedAtUtc
            FROM dbo.Configurations WHERE IsActive = 1 AND UpdatedAt >@since";

            using var conn = await OpenForAppAsync(applicationName, cancellationToken);
            var result = await conn.QueryAsync<ConfigurationItem>(new CommandDefinition(
                sql, new { app = applicationName, since = sinceUtc }, cancellationToken: cancellationToken));

            return result.ToList();
        }

        public async Task<int>UpsertAsync(ConfigurationItem item, CancellationToken cancellationToken=default)
        {
            const string sql = @"
            MERGE dbo.Configurations WITH (HOLDLOCK) AS target
            USING (SELECT @ApplicationName AS ApplicationName, @Name AS Name) AS src
            ON (
              UPPER(LTRIM(RTRIM(target.ApplicationName))) = UPPER(LTRIM(RTRIM(src.ApplicationName))) AND
              UPPER(LTRIM(RTRIM(target.Name)))            = UPPER(LTRIM(RTRIM(src.Name))) AND
              target.IsActive = 1
            )
            WHEN MATCHED THEN
              UPDATE SET
                target.[Type] = @Type,
                target.[Value] = @Value,
                target.UpdatedAt = SYSUTCDATETIME()
            WHEN NOT MATCHED THEN
              INSERT (ApplicationName, Name, [Type], [Value], IsActive, UpdatedAt)
              VALUES (@ApplicationName, @Name, @Type, @Value, 1, SYSUTCDATETIME());
            SELECT @@ROWCOUNT;";
            using var conn = await OpenForAppAsync(item.ApplicationName, cancellationToken);
            return await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                sql,new {
                item.ApplicationName,
                item.Name,
                Type = item.Type.ToString().ToLowerInvariant(),
                item.Value
            }, cancellationToken: cancellationToken));
        }

        public async Task<int> DeactivateAsync(string applicationName,string name,CancellationToken cancellationToken=default)
        {
            const string sql = @"UPDATE dbo.Configurations SET IsActive = 0 ,UpdatedAt = SYSUTCDATETIME() 
            WHERE ApplicationName=@app AND Name=@name AND IsActive = 1;
            SELECT @@ROWCOUNT";

            using var conn = await OpenForAppAsync(applicationName, cancellationToken);
            return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql,new
            {
                app = applicationName,
                name
            }, cancellationToken: cancellationToken));

        }

        public async Task<IReadOnlyList<ConfigurationItem>> GetByApplicationAsync(string applicationName, CancellationToken ct = default)
        {
            const string sql = @"
SELECT Id, ApplicationName, Name, Type, Value, IsActive, UpdatedAt AS UpdatedAtUtc
FROM dbo.Configurations
ORDER BY Id;";

            using var conn = await OpenForAppAsync(applicationName, ct);
            var rows = await conn.QueryAsync<ConfigurationItem>(new CommandDefinition(
                sql, new { app = applicationName }, cancellationToken: ct));
            return rows.ToList();
        }


        public async Task<IReadOnlyList<ConfigurationItem>> GetAllAsync(CancellationToken ct = default)
        {
            const string sql = @"
    SELECT Id, ApplicationName, Name, Type, Value, IsActive, UpdatedAt AS UpdatedAtUtc
    FROM dbo.Configurations
    ORDER BY Id;";
            using var conn = Create();
            var rows = await conn.QueryAsync<ConfigurationItem>(new CommandDefinition(sql, cancellationToken: ct));
            return rows.ToList();
        }

        public async Task<IReadOnlyList<string>> GetApplicationsAsync(CancellationToken ct = default)
        {
            const string sql = @"SELECT DISTINCT ApplicationName FROM dbo.Configurations;";
            using var conn = Create();
            var rows = await conn.QueryAsync<string>(new CommandDefinition(sql, cancellationToken: ct));
            return rows.ToList();
        }

        public async Task<int> ActivateAsync(string applicationName, string name, CancellationToken cancellationToken = default)
        {
            const string sql = @"UPDATE dbo.Configurations SET IsActive = 1 ,UpdatedAt = SYSUTCDATETIME() 
            WHERE ApplicationName=@app AND Name=@name AND IsActive = 0;
            SELECT @@ROWCOUNT";
            using var conn = await OpenForAppAsync(applicationName, cancellationToken);
            return await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, new
            {
                app = applicationName,
                name
            }, cancellationToken: cancellationToken));
        }
    }
}
