namespace LifeInsuranceCRM.Core.Abstractions.Services;

/// <summary>
/// Provides the current UTC instant for audit timestamps and event recording.
/// </summary>
public interface INowProvider
{
    DateTimeOffset UtcNow { get; }
}
