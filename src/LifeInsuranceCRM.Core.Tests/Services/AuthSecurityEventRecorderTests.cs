using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace LifeInsuranceCRM.Core.Tests.Services;

public class AuthSecurityEventRecorderTests
{
    [Fact]
    public async Task RecordAsync_WhenRepositoryThrows_LogsErrorAndDoesNotThrow()
    {
        var repository = new Mock<IAuthSecurityEventRepository>();
        repository
            .Setup(r => r.RecordAsync(It.IsAny<AuthSecurityEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("insert failed"));

        var logger = new RecordingLogger<AuthSecurityEventRecorder>();
        var recorder = CreateRecorder(repository.Object, logger);

        await recorder.RecordAsync(
            AuthSecurityEventTypes.Unauthorized,
            success: false,
            failureReason: "Unauthorized");

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains(AuthSecurityEventTypes.Unauthorized, entry.Message, StringComparison.Ordinal);
        Assert.IsType<InvalidOperationException>(entry.Exception);
    }

    [Fact]
    public async Task RecordAsync_WhenSuccessful_DoesNotLog()
    {
        var repository = new Mock<IAuthSecurityEventRepository>();
        var logger = new RecordingLogger<AuthSecurityEventRecorder>();
        var recorder = CreateRecorder(repository.Object, logger);

        await recorder.RecordAsync(AuthSecurityEventTypes.TenantResolved, success: true);

        Assert.Empty(logger.Entries);
        repository.Verify(
            r => r.RecordAsync(It.IsAny<AuthSecurityEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static AuthSecurityEventRecorder CreateRecorder(
        IAuthSecurityEventRepository repository,
        ILogger<AuthSecurityEventRecorder> logger)
    {
        var actorTracker = new Mock<IActorTracker>();
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var nowProvider = new Mock<INowProvider>();
        nowProvider.Setup(n => n.UtcNow).Returns(DateTimeOffset.UtcNow);

        return new AuthSecurityEventRecorder(
            repository,
            actorTracker.Object,
            httpContextAccessor.Object,
            nowProvider.Object,
            logger);
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
