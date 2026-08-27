using LifeInsuranceCRM.API.ExceptionHandling;
using LifeInsuranceCRM.API.Services;
using LifeInsuranceCRM.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LifeInsuranceCRM.API.Tests.ExceptionHandling;

public class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_LogsSanitizedExceptionWithoutMedicareNumber()
    {
        var logger = new RecordingLogger<GlobalExceptionHandler>();
        var handler = new GlobalExceptionHandler(new ProblemDetailsFactory(), logger);
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        await handler.TryHandleAsync(
            context,
            new InvalidOperationException("MedicareNumber=1EG4-TE5-MK72"),
            CancellationToken.None);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.IsType<PiiSanitizedException>(entry.Exception);
        Assert.DoesNotContain("1EG4-TE5-MK72", entry.Exception!.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("1EG4-TE5-MK72", entry.Message, StringComparison.Ordinal);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    private sealed class RecordingLogger<T> : ILogger<T>
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
