namespace LifeInsuranceCRM.Core.Models;

public sealed record AuditStamp(Guid UserId, DateTimeOffset Timestamp);
