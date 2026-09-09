using LifeInsuranceCRM.API.ExceptionHandling;
using LifeInsuranceCRM.API.Services;
using LifeInsuranceCRM.Core.Constants;
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

    [Fact]
    public async Task TryHandleAsync_PayloadTooLarge_Returns413()
    {
        var logger = new RecordingLogger<GlobalExceptionHandler>();
        var handler = new GlobalExceptionHandler(new ProblemDetailsFactory(), logger);
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        await handler.TryHandleAsync(
            context,
            new BadHttpRequestException("Request body too large.", StatusCodes.Status413PayloadTooLarge),
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, context.Response.StatusCode);
        var body = await ReadBodyAsync(context);
        Assert.Contains(ImportErrorCodes.PayloadTooLarge, body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(UseCaseStatus.InvalidRequest, StatusCodes.Status400BadRequest)]
    [InlineData(UseCaseStatus.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(UseCaseStatus.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(UseCaseStatus.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(UseCaseStatus.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(UseCaseStatus.Failure, StatusCodes.Status500InternalServerError)]
    public async Task TryHandleAsync_CrmException_MapsUseCaseStatus(UseCaseStatus status, int expectedStatus)
    {
        var logger = new RecordingLogger<GlobalExceptionHandler>();
        var handler = new GlobalExceptionHandler(new ProblemDetailsFactory(), logger);
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        await handler.TryHandleAsync(
            context,
            new CrmException(status, "mapped", "edge.test"),
            CancellationToken.None);

        Assert.Equal(expectedStatus, context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_Non413BadHttpRequest_Returns500()
    {
        var logger = new RecordingLogger<GlobalExceptionHandler>();
        var handler = new GlobalExceptionHandler(new ProblemDetailsFactory(), logger);
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        await handler.TryHandleAsync(
            context,
            new BadHttpRequestException("Malformed request."),
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_OperationCanceled_Returns500AndDoesNotThrow()
    {
        var logger = new RecordingLogger<GlobalExceptionHandler>();
        var handler = new GlobalExceptionHandler(new ProblemDetailsFactory(), logger);
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        var handled = await handler.TryHandleAsync(
            context,
            new OperationCanceledException("client disconnected"),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_CanceledToken_SurfacesCancellation()
    {
        var logger = new RecordingLogger<GlobalExceptionHandler>();
        var handler = new GlobalExceptionHandler(new ProblemDetailsFactory(), logger);
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            handler.TryHandleAsync(
                context,
                new InvalidOperationException("boom"),
                cts.Token).AsTask());
    }

    [Fact]
    public async Task TryHandleAsync_SpecialCharactersInMessage_AreWrittenWithoutCrashing()
    {
        var logger = new RecordingLogger<GlobalExceptionHandler>();
        var handler = new GlobalExceptionHandler(new ProblemDetailsFactory(), logger);
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        await handler.TryHandleAsync(
            context,
            new CrmException(UseCaseStatus.InvalidRequest, "Name <O'Brien & 李>", "client.first_name.required"),
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        var body = await ReadBodyAsync(context);
        Assert.Contains("O'Brien", body, StringComparison.Ordinal);
    }

    private static async Task<string> ReadBodyAsync(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, leaveOpen: true);
        return await reader.ReadToEndAsync();
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
