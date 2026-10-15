namespace LifeInsuranceCRM.Core.Entities;

public sealed class ImportMappingProfile
{
    public Guid ImportMappingProfileId { get; init; }

    public Guid TenantId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Definition { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }

    public Guid CreatedByUserId { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public Guid UpdatedByUserId { get; init; }
}
