using Microsoft.Data.SqlClient;

namespace LifeInsuranceCRM.Core.Abstractions.Data;

public interface IDbExecutor
{
    /// <summary>
    /// Lets the next commands on this executor read every tenant. SuperAdmin platform queries only.
    /// </summary>
    IDisposable BypassTenantFilter();

    Task<int> ExecuteNonQueryAsync(
        string sql,
        CancellationToken cancellationToken = default,
        params SqlParameter[] parameters);

    Task<T?> ExecuteScalarAsync<T>(
        string sql,
        CancellationToken cancellationToken = default,
        params SqlParameter[] parameters);

    Task ExecuteReaderAsync(
        string sql,
        Func<SqlDataReader, CancellationToken, Task> read,
        CancellationToken cancellationToken = default,
        params SqlParameter[] parameters);
}
