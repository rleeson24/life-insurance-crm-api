using System.Diagnostics;

namespace LifeInsuranceCRM.Utilities;

public static class TelemetryConstants
{
    public const string ActivitySourceName = "LifeInsuranceCRM";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    /// <summary>Replacement text for redacted PHI and omitted SQL.</summary>
    public const string RedactedPlaceholder = "[REDACTED]";

    /// <summary>
    /// Set on spans whose SQL may include Medicare number or date of birth.
    /// Processors must omit <c>db.query.text</c> / <c>db.statement</c> when this is true.
    /// </summary>
    public const string ContainsPhiSqlTag = "licrm.phi.sql";

    /// <summary>True when SQL command text was stripped before export.</summary>
    public const string DbStatementOmittedTag = "licrm.db.statement.omitted";
}
