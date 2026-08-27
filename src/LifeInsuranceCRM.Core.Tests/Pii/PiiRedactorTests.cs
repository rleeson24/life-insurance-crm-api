using LifeInsuranceCRM.Utilities;
using Microsoft.Extensions.Logging;

namespace LifeInsuranceCRM.Core.Tests.Pii;

public class PiiRedactorTests
{
    [Theory]
    [InlineData("""{"MedicareNumber":"1EG4-TE5-MK72"}""", "1EG4-TE5-MK72")]
    [InlineData("""{"medicareNumber": "1EG4TE5MK72"}""", "1EG4TE5MK72")]
    [InlineData("@MedicareNumber = '1EG4-TE5-MK72'", "1EG4-TE5-MK72")]
    [InlineData("DateOfBirth: 1950-06-15", "1950-06-15")]
    [InlineData("""{"DateOfBirth":"1950-06-15"}""", "1950-06-15")]
    public void Redact_MasksNamedPhiValues(string input, string secret)
    {
        var redacted = PiiRedactor.Redact(input);

        Assert.DoesNotContain(secret, redacted, StringComparison.Ordinal);
        Assert.Contains(TelemetryConstants.RedactedPlaceholder, redacted);
        Assert.DoesNotContain($"{TelemetryConstants.RedactedPlaceholder}]", redacted);
    }

    [Fact]
    public void Redact_QuotedJsonKeepsQuotesAroundPlaceholder()
    {
        var redacted = PiiRedactor.Redact("""{"DateOfBirth":"1950-06-15","MedicareNumber":"1EG4-TE5-MK72"}""");

        Assert.Equal(
            """{"DateOfBirth":"[REDACTED]","MedicareNumber":"[REDACTED]"}""",
            redacted);
    }

    [Fact]
    public void Redact_IsIdempotent_DoesNotStackClosingBrackets()
    {
        var once = PiiRedactor.Redact("""{"DateOfBirth":"1950-06-15","MedicareNumber":"1EG4-TE5-MK72"}""");
        var twice = PiiRedactor.Redact(once);

        Assert.Equal(once, twice);
        Assert.DoesNotContain("[REDACTED]]", twice);
    }

    [Fact]
    public void Redact_MasksStandaloneMedicareBeneficiaryId()
    {
        var redacted = PiiRedactor.Redact("Lookup failed for 1EG4-TE5-MK72");

        Assert.DoesNotContain("1EG4-TE5-MK72", redacted);
        Assert.Equal($"Lookup failed for {TelemetryConstants.RedactedPlaceholder}", redacted);
    }

    [Fact]
    public void Redact_LeavesAuthEmailIntact()
    {
        const string email = "dev-user@localhost";
        Assert.Equal(email, PiiRedactor.Redact(email));
    }

    [Fact]
    public void Redact_LeavesUnrelatedIsoDatesIntact()
    {
        const string timestamp = "CreatedAt=2026-08-26T12:00:00Z";
        Assert.Equal(timestamp, PiiRedactor.Redact(timestamp));
    }

    [Fact]
    public void ToSanitizedException_DoesNotLeakMedicareNumberOrOriginalInnerException()
    {
        var inner = new InvalidOperationException("MedicareNumber=1EG4-TE5-MK72");
        var outer = new InvalidOperationException("Failed to save client", inner);

        var sanitized = PiiRedactor.ToSanitizedException(outer);

        Assert.IsType<PiiSanitizedException>(sanitized);
        Assert.DoesNotContain("1EG4-TE5-MK72", sanitized.ToString(), StringComparison.Ordinal);
        Assert.Null(sanitized.InnerException);
        Assert.Equal(typeof(InvalidOperationException).FullName, ((PiiSanitizedException)sanitized).OriginalExceptionType);
    }

    [Theory]
    [InlineData("MedicareNumber")]
    [InlineData("dateOfBirth")]
    [InlineData("@DateOfBirth")]
    [InlineData("db.query.text")]
    [InlineData("db.query.parameter.MedicareNumber")]
    public void IsPhiAttributeKey_DetectsSensitiveKeys(string key)
    {
        Assert.True(PiiRedactor.IsPhiAttributeKey(key));
    }

    [Fact]
    public void IsPhiAttributeKey_IgnoresEmail()
    {
        Assert.False(PiiRedactor.IsPhiAttributeKey("UserEmail"));
    }
}

public class PiiSanitizingLoggerTests
{
    [Fact]
    public void Log_RedactsFormattedMessageAndException()
    {
        var inner = new RecordingLogger();
        var logger = new PiiSanitizingLogger(inner);

        logger.LogError(
            new InvalidOperationException("MedicareNumber=1EG4-TE5-MK72"),
            "Saving {MedicareNumber}",
            "1EG4-TE5-MK72");

        var entry = Assert.Single(inner.Entries);
        Assert.DoesNotContain("1EG4-TE5-MK72", entry.Message);
        Assert.IsType<PiiSanitizedException>(entry.Exception);
        Assert.DoesNotContain("1EG4-TE5-MK72", entry.Exception!.ToString());
    }

    private sealed class RecordingLogger : ILogger
    {
        public List<(LogLevel Level, Exception? Exception, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, exception, formatter(state, exception)));
        }
    }
}
