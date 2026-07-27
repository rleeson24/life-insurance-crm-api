namespace LifeInsuranceCRM.Core.Config;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string ConnectionString { get; set; } = string.Empty;

    public string Server { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}
