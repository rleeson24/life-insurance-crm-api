using System.Text;

namespace LifeInsuranceCRM.AppHost;

internal static class LiveSchemaScripts
{
    private const string ScriptRoot = "database/live";

    private static readonly string[] ScriptFiles =
    [
        "001_Tenants.sql",
        "002_Clients.sql",
        "003_ClientInteractions.sql",
        "004_MedicareEnrollments.sql",
        "005_SupplementalEnrollments.sql",
        "006_OrganizationUsers.sql",
        "007_AuthSecurityEvents.sql",
        "008_RLS.sql",
        "009_OrganizationUserRoles.sql",
        "seed/001_DevelopmentTenant.sql",
    ];

    public static string BuildCreationScript(string databaseName)
    {
        var scriptDirectory = Path.Combine(AppContext.BaseDirectory, ScriptRoot);
        var script = new StringBuilder();
        script.AppendLine(
            $"""
            IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'{databaseName}')
            BEGIN
                CREATE DATABASE [{databaseName}];
            END
            GO
            USE [{databaseName}];
            GO
            """);

        foreach (var scriptFile in ScriptFiles)
        {
            var scriptPath = Path.Combine(scriptDirectory, scriptFile);
            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException($"Live schema script not found: {scriptPath}", scriptPath);
            }

            script.AppendLine(File.ReadAllText(scriptPath));
            script.AppendLine();
        }

        return script.ToString();
    }
}
