using LifeInsuranceCRM.Core.Abstractions.Services;

namespace LifeInsuranceCRM.Core.Services;

public sealed class UtcNowProvider : INowProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
