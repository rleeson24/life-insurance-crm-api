IF SCHEMA_ID(N'migrate') IS NULL
    EXEC(N'CREATE SCHEMA migrate;');
GO

IF OBJECT_ID(N'dbo.fn_TenantFilter', N'IF') IS NOT NULL
    DROP FUNCTION dbo.fn_TenantFilter;
GO

CREATE FUNCTION dbo.fn_TenantFilter(@TenantId uniqueidentifier)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN SELECT 1 AS fn_TenantFilter_Result
WHERE @TenantId = TRY_CAST(SESSION_CONTEXT(N'TenantId') AS uniqueidentifier);
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy')
    DROP SECURITY POLICY dbo.TenantPolicy;
GO

CREATE SECURITY POLICY dbo.TenantPolicy
    ADD FILTER PREDICATE dbo.fn_TenantFilter(TenantId) ON dbo.Clients,
    ADD FILTER PREDICATE dbo.fn_TenantFilter(TenantId) ON dbo.ClientInteractions,
    ADD FILTER PREDICATE dbo.fn_TenantFilter(TenantId) ON dbo.MedicareEnrollments,
    ADD FILTER PREDICATE dbo.fn_TenantFilter(TenantId) ON dbo.SupplementalEnrollments,
    ADD FILTER PREDICATE dbo.fn_TenantFilter(TenantId) ON dbo.AuthSecurityEvents
WITH (STATE = ON);
GO

-- OrganizationUsers is excluded from RLS: tenant resolution queries by UserId before SESSION_CONTEXT is set.
