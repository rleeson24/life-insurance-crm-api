using System.Diagnostics.CodeAnalysis;

namespace LifeInsuranceCRM.Core.Models;

[ExcludeFromCodeCoverage]

public sealed record AuditStamp(Guid UserId, DateTimeOffset Timestamp);