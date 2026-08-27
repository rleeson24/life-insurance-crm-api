using Microsoft.Extensions.Logging;

namespace LifeInsuranceCRM.Utilities;

/// <summary>
/// <see cref="ILogger"/> decorator that redacts PHI from formatted messages and exceptions
/// before they reach console, debug, or OpenTelemetry sinks.
/// </summary>
public sealed class PiiSanitizingLogger : ILogger
{
    private readonly ILogger _inner;

    public PiiSanitizingLogger(ILogger inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
        _inner.BeginScope(state is string text ? (TState)(object)PiiRedactor.Redact(text)! : state);

    public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var redactedException = exception is null ? null : PiiRedactor.ToSanitizedException(exception);
        var message = PiiRedactor.Redact(formatter(state, exception)) ?? string.Empty;
        _inner.Log(logLevel, eventId, message, redactedException, static (s, _) => s);
    }
}

public sealed class PiiSanitizingLoggerFactory : ILoggerFactory
{
    private readonly ILoggerFactory _inner;

    public PiiSanitizingLoggerFactory(ILoggerFactory inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    public void AddProvider(ILoggerProvider provider) => _inner.AddProvider(provider);

    public ILogger CreateLogger(string categoryName) =>
        new PiiSanitizingLogger(_inner.CreateLogger(categoryName));

    public void Dispose() => _inner.Dispose();
}
