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

-- 015 creates ImportMappingProfiles after this policy is rebuilt. Re-running 013
-- on a database that already has the table must put the predicate back.
IF OBJECT_ID(N'dbo.ImportMappingProfiles', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy')
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates sp
        WHERE sp.object_id = OBJECT_ID(N'dbo.TenantPolicy')
          AND sp.target_object_id = OBJECT_ID(N'dbo.ImportMappingProfiles')
          AND sp.predicate_type = 0
   )
BEGIN
    -- ALTER SECURITY POLICY is bound at compile time, so a direct statement fails
    -- when the predicate already exists even inside IF NOT EXISTS.
    EXEC(N'ALTER SECURITY POLICY dbo.TenantPolicy ADD FILTER PREDICATE dbo.fn_TenantFilter(TenantId) ON dbo.ImportMappingProfiles;');
END
GO
