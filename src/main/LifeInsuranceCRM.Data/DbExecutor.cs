using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Config;
using LifeInsuranceCRM.Core.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace LifeInsuranceCRM.Data;

public sealed class DbExecutor : IDbExecutor
{
    private readonly DatabaseOptions _options;

    public DbExecutor(IOptions<DatabaseOptions> options)
    {
        _options = options.Value;
    }

    public async Task<int> ExecuteNonQueryAsync(
        string sql,
        CancellationToken cancellationToken = default,
        params SqlParameter[] parameters)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, sql, parameters);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<T?> ExecuteScalarAsync<T>(
        string sql,
        CancellationToken cancellationToken = default,
        params SqlParameter[] parameters)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, sql, parameters);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null or DBNull)
        {
            return default;
        }

        return (T)Convert.ChangeType(result, typeof(T));
    }

    public async Task ExecuteReaderAsync(
        string sql,
        Func<SqlDataReader, CancellationToken, Task> read,
        CancellationToken cancellationToken = default,
        params SqlParameter[] parameters)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, sql, parameters);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await read(reader, cancellationToken);
    }

    public Task SetTenantSessionContextAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        ExecuteNonQueryAsync(
            "EXEC sys.sp_set_session_context @key = N'TenantId', @value = @tenantId, @read_only = 1;",
            cancellationToken,
            new SqlParameter("@tenantId", tenantId));

    private SqlConnection CreateConnection() => new(_options.ConnectionString);

    private static SqlCommand CreateCommand(SqlConnection connection, string sql, SqlParameter[] parameters)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        if (parameters.Length > 0)
        {
            command.Parameters.AddRange(
                parameters.Select(static p => new SqlParameter(p.ParameterName, p.Value)).ToArray());
        }

        return command;
    }
}
