using System.Net;
using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Abstractions.Data;
using LifeInsuranceCRM.Core.Abstractions.Services;
using LifeInsuranceCRM.Core.Config;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Core.Entities;
using LifeInsuranceCRM.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

    [Fact]
    public async Task RecordAsync_WhenTrustingForwardedFor_StoresLastAddressAndAccessFacts()
    {
        AuthSecurityEvent? captured = null;
        var repository = new Mock<IAuthSecurityEventRepository>();
        repository
            .Setup(r => r.RecordAsync(It.IsAny<AuthSecurityEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AuthSecurityEvent, CancellationToken>((securityEvent, _) => captured = securityEvent)
            .Returns(Task.CompletedTask);

        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.4");
        httpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.9, 198.51.100.20";
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var targetId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var recorder = CreateRecorder(repository.Object, new RecordingLogger<AuthSecurityEventRecorder>(), accessor, trustForwardedFor: true);

        await recorder.RecordAsync(
            AuthSecurityEventTypes.ClientViewed,
            success: true,
            resource: "clients",
            httpStatus: 200,
            resultCount: 1,
            targetId: targetId,
            detail: "page=1;pageSize=25;total=40");

        Assert.NotNull(captured);
        Assert.Equal("198.51.100.20", captured.IpAddress);
        Assert.Equal(200, captured.HttpStatus);
        Assert.Equal(1, captured.ResultCount);
        Assert.Equal(targetId, captured.TargetId);
        Assert.Equal("page=1;pageSize=25;total=40", captured.Detail);
        Assert.Equal("clients", captured.Resource);
    }

    [Fact]
    public async Task RecordAsync_WhenForwardedForIsNotTrusted_UsesRemoteAddress()
    {
        AuthSecurityEvent? captured = null;
        var repository = new Mock<IAuthSecurityEventRepository>();
        repository
            .Setup(r => r.RecordAsync(It.IsAny<AuthSecurityEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AuthSecurityEvent, CancellationToken>((securityEvent, _) => captured = securityEvent)
            .Returns(Task.CompletedTask);

        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.4");
        httpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.9";
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var recorder = CreateRecorder(
            repository.Object,
            new RecordingLogger<AuthSecurityEventRecorder>(),
            accessor,
            trustForwardedFor: false);

        await recorder.RecordAsync(AuthSecurityEventTypes.Unauthorized, success: false);

        Assert.Equal("10.0.0.4", captured!.IpAddress);
    }

    private static AuthSecurityEventRecorder CreateRecorder(
        IAuthSecurityEventRepository repository,
        ILogger<AuthSecurityEventRecorder> logger)
    {
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        return CreateRecorder(repository, logger, httpContextAccessor.Object, trustForwardedFor: false);
    }

    private static AuthSecurityEventRecorder CreateRecorder(
        IAuthSecurityEventRepository repository,
        ILogger<AuthSecurityEventRecorder> logger,
        IHttpContextAccessor httpContextAccessor,
        bool trustForwardedFor)
    {
        var actorTracker = new Mock<IActorTracker>();
        var nowProvider = new Mock<INowProvider>();
        nowProvider.Setup(n => n.UtcNow).Returns(DateTimeOffset.UtcNow);

        return new AuthSecurityEventRecorder(
            repository,
            actorTracker.Object,
            httpContextAccessor,
            nowProvider.Object,
            logger,
            Options.Create(new ClientIpOptions { TrustForwardedFor = trustForwardedFor }));
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
