-- SuperAdmin platform queries set SESSION_CONTEXT('BypassTenantFilter') so they can
-- read client counts and auth security events across organizations.
-- The filter function is schema-bound and referenced by TenantPolicy, so the policy
-- must be dropped before the function can be replaced.

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy')
    DROP SECURITY POLICY dbo.TenantPolicy;
GO

CREATE OR ALTER FUNCTION dbo.fn_TenantFilter(@TenantId uniqueidentifier)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN SELECT 1 AS fn_TenantFilter_Result
WHERE @TenantId = TRY_CAST(SESSION_CONTEXT(N'TenantId') AS uniqueidentifier)
   OR TRY_CAST(SESSION_CONTEXT(N'BypassTenantFilter') AS bit) = 1;
GO

IF NOT EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy')
BEGIN
    CREATE SECURITY POLICY dbo.TenantPolicy
        ADD FILTER PREDICATE dbo.fn_TenantFilter(TenantId) ON dbo.Clients,
        ADD FILTER PREDICATE dbo.fn_TenantFilter(TenantId) ON dbo.ClientInteractions,
        ADD FILTER PREDICATE dbo.fn_TenantFilter(TenantId) ON dbo.MajorMedicalEnrollments,
        ADD FILTER PREDICATE dbo.fn_TenantFilter(TenantId) ON dbo.SecondaryEnrollments,
        ADD FILTER PREDICATE dbo.fn_TenantFilter(TenantId) ON dbo.DrugPlanEnrollments,
        ADD FILTER PREDICATE dbo.fn_TenantFilter(TenantId) ON dbo.AuthSecurityEvents,
        ADD FILTER PREDICATE dbo.fn_TenantFilter(TenantId) ON dbo.MedicarePlanNames,
        ADD FILTER PREDICATE dbo.fn_TenantFilter(TenantId) ON dbo.DrugPlanNames,
        ADD FILTER PREDICATE dbo.fn_TenantFilter(TenantId) ON dbo.SecondaryPlanNames
    WITH (STATE = ON);
END
GO
