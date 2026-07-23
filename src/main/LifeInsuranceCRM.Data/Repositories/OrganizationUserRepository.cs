using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Models;
using Microsoft.Data.SqlClient;

namespace LifeInsuranceCRM.Data.Repositories;

public sealed class OrganizationUserRepository : IOrganizationUserRepository
{
    private const string SelectUserContextSql = """
        SELECT TOP (1) TenantId, Role, IsActive
        FROM dbo.OrganizationUsers
        WHERE UserId = @UserId AND IsDeleted = 0;
        """;

    private readonly IDbExecutor _dbExecutor;

    public OrganizationUserRepository(IDbExecutor dbExecutor)
    {
        _dbExecutor = dbExecutor;
    }

    public async Task<OrganizationUserContext?> GetUserContextAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        OrganizationUserContext? userContext = null;

        await _dbExecutor.ExecuteReaderAsync(
            SelectUserContextSql,
            async (reader, ct) =>
            {
                if (await reader.ReadAsync(ct))
                {
                    userContext = new OrganizationUserContext(
                        reader.GetGuid(0),
                        reader.GetString(1),
                        reader.GetBoolean(2));
                }
            },
            cancellationToken,
            new SqlParameter("@UserId", userId));

        return userContext;
    }
}
