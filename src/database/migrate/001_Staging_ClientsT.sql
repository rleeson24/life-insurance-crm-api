-- Phase 2: mirrors Access ClientsT (1:1). Not used in Phase 0/1 runtime.
IF SCHEMA_ID(N'migrate') IS NULL
    EXEC(N'CREATE SCHEMA migrate;');
GO
