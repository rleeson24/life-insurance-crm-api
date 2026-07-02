using LifeInsuranceCRM.Core.Abstractions.Data;
using Microsoft.Data.SqlClient;

namespace LifeInsuranceCRM.Data.Repositories;

public sealed class OrganizationUserRepository : IOrganizationUserRepository
{
    private const string SelectTenantSql = """
        SELECT TOP (1) TenantId
        FROM dbo.OrganizationUsers
        WHERE UserId = @UserId AND IsDeleted = 0;
        """;

    private readonly IDbExecutor _dbExecutor;

    public OrganizationUserRepository(IDbExecutor dbExecutor)
    {
        _dbExecutor = dbExecutor;
    }

    public async Task<Guid?> GetTenantIdForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        Guid? tenantId = null;

        await _dbExecutor.ExecuteReaderAsync(
            SelectTenantSql,
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    tenantId = reader.GetGuid(0);
                }
            },
            cancellationToken,
            new SqlParameter("@UserId", userId));

        return tenantId;
    }
}
