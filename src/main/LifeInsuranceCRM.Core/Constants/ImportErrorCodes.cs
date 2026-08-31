namespace LifeInsuranceCRM.Core.Constants;

public static class ImportErrorCodes
{
    public const string ActorNotAuthenticated = "actor.not_authenticated";
    public const string ActorNotAdmin = "import.actor.not_admin";
    public const string TenantNotEmpty = "import.tenant.not_empty";
    public const string NoClients = "import.no_clients";
    public const string InProgress = "import.in_progress";
    public const string PayloadTooLarge = "import.payload.too_large";
}
