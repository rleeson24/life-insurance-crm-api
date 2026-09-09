using LifeInsuranceCRM.API.Auth;
using LifeInsuranceCRM.API.Services;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LifeInsuranceCRM.API.Tests.Services;

public class ProcessResponseActionMapperTests
{
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _tenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Map_GetSuccess_DoesNotLog()
    {
        var logger = new RecordingLogger<ProcessResponseActionMapper>();
        var mapper = CreateMapper(logger);
        var context = CreateContext("GET", "/api/clients");

        var result = mapper.Map(ProcessResponse<int>.Succeeded(1), context);

        Assert.IsType<OkObjectResult>(result);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void Map_PostSuccess_LogsInformationWithPathAndActor()
    {
        var logger = new RecordingLogger<ProcessResponseActionMapper>();
        var mapper = CreateMapper(logger);
        var context = CreateContext("POST", "/api/clients");

        mapper.Map(ProcessResponse<int>.Succeeded(1), context);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains("POST", entry.Message, StringComparison.Ordinal);
        Assert.Contains("/api/clients", entry.Message, StringComparison.Ordinal);
        Assert.Contains(_userId.ToString(), entry.Message, StringComparison.Ordinal);
        Assert.Contains(_tenantId.ToString(), entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Map_NotFound_LogsInformation()
    {
        var logger = new RecordingLogger<ProcessResponseActionMapper>();
        var mapper = CreateMapper(logger);
        var context = CreateContext("GET", "/api/clients/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var result = mapper.Map(
            ProcessResponse<int>.WithStatus(UseCaseStatus.NotFound, "Client not found", ClientErrorCodes.ClientNotFound),
            context);

        Assert.Equal(StatusCodes.Status404NotFound, ((ObjectResult)result).StatusCode);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains(ClientErrorCodes.ClientNotFound, entry.Message, StringComparison.Ordinal);
        Assert.Contains("Client not found", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Map_InvalidRequest_LogsDebug()
    {
        var logger = new RecordingLogger<ProcessResponseActionMapper>();
        var mapper = CreateMapper(logger);
        var context = CreateContext("POST", "/api/clients");

        mapper.Map(
            ProcessResponse<int>.InvalidRequestResponse("First name is required", ClientErrorCodes.FirstNameRequired),
            context);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Debug, entry.Level);
        Assert.Contains(ClientErrorCodes.FirstNameRequired, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Map_Forbidden_LogsWarning()
    {
        var logger = new RecordingLogger<ProcessResponseActionMapper>();
        var mapper = CreateMapper(logger);
        var context = CreateContext("DELETE", "/api/clients/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        mapper.Map(
            ProcessResponse<int>.WithStatus(UseCaseStatus.Forbidden, "Forbidden", "access.forbidden"),
            context);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("Forbidden", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Map_Unauthorized_LogsWarningAndReturns401()
    {
        var logger = new RecordingLogger<ProcessResponseActionMapper>();
        var mapper = CreateMapper(logger);
        var context = CreateContext("GET", "/api/clients");

        var result = mapper.Map(
            ProcessResponse<int>.WithStatus(UseCaseStatus.Unauthorized, "Authentication required", "actor.not_authenticated"),
            context);

        Assert.Equal(StatusCodes.Status401Unauthorized, ((ObjectResult)result).StatusCode);
        Assert.Equal(LogLevel.Warning, Assert.Single(logger.Entries).Level);
    }

    [Fact]
    public void Map_Conflict_LogsInformation()
    {
        var logger = new RecordingLogger<ProcessResponseActionMapper>();
        var mapper = CreateMapper(logger);
        var context = CreateContext("POST", "/api/plan-names");

        mapper.Map(
            ProcessResponse<int>.WithStatus(UseCaseStatus.Conflict, "That plan name already exists", "plan_name.name.already_exists"),
            context);

        Assert.Equal(LogLevel.Information, Assert.Single(logger.Entries).Level);
    }

    [Fact]
    public void Map_Failure_LogsWarningAndReturns500()
    {
        var logger = new RecordingLogger<ProcessResponseActionMapper>();
        var mapper = CreateMapper(logger);
        var context = CreateContext("GET", "/api/reports/book");

        var result = mapper.Map(
            ProcessResponse<int>.WithStatus(UseCaseStatus.Failure, "Query failed", "unexpected_error"),
            context);

        Assert.Equal(StatusCodes.Status500InternalServerError, ((ObjectResult)result).StatusCode);
        Assert.Equal(LogLevel.Warning, Assert.Single(logger.Entries).Level);
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public void Map_MutatingSuccess_LogsInformation(string method)
    {
        var logger = new RecordingLogger<ProcessResponseActionMapper>();
        var mapper = CreateMapper(logger);
        var context = CreateContext(method, "/api/clients/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        mapper.Map(ProcessResponse<int>.Succeeded(1), context);

        Assert.Equal(LogLevel.Information, Assert.Single(logger.Entries).Level);
    }

    [Fact]
    public void Map_InvalidRequest_WithSpecialCharactersInPath_DoesNotThrow()
    {
        var logger = new RecordingLogger<ProcessResponseActionMapper>();
        var mapper = CreateMapper(logger);
        var context = CreateContext("POST", "/api/clients/%3Cscript%3E");

        var result = mapper.Map(
            ProcessResponse<int>.InvalidRequestResponse("Name O'Brien & 李", ClientErrorCodes.FirstNameRequired),
            context);

        Assert.Equal(StatusCodes.Status400BadRequest, ((ObjectResult)result).StatusCode);
    }

    private ProcessResponseActionMapper CreateMapper(RecordingLogger<ProcessResponseActionMapper> logger)
    {
        var actorTracker = new ActorTracker();
        actorTracker.SetActor(_userId, "dev-user@localhost", _tenantId, OrganizationRoles.Admin);
        return new ProcessResponseActionMapper(new ProblemDetailsFactory(), actorTracker, logger);
    }

    private static DefaultHttpContext CreateContext(string method, string path) =>
        new()
        {
            Request =
            {
                Method = method,
                Path = path,
            },
        };

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
