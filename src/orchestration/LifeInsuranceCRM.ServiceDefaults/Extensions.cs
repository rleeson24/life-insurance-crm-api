using Azure.Monitor.OpenTelemetry.AspNetCore;
using LifeInsuranceCRM.Core.Config;
using LifeInsuranceCRM.ServiceDefaults;
using LifeInsuranceCRM.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

// Adds common Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.Services.AddPiiSanitizingLoggerFactory();
        builder.AddDefaultHealthChecks();
        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var isDevelopment = builder.Environment.IsDevelopment();

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = isDevelopment;
            logging.IncludeScopes = true;
            logging.ParseStateValues = true;
            logging.AddProcessor(new PiiRedactingLogProcessor());
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(TelemetryConstants.ActivitySourceName)
                    .AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(options =>
                        options.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath))
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation(options =>
                    {
                        // Query parameter capture stays off (library default). A processor
                        // still strips db.query.text / db.query.parameter.* before export.
                        options.EnrichWithSqlCommand = (activity, command) =>
                        {
                            if (CommandContainsPhi(command))
                            {
                                activity.SetTag(TelemetryConstants.ContainsPhiSqlTag, true);
                            }
                        };
                    })
                    .AddProcessor(new SqlStatementRedactingProcessor(omitSqlText: !isDevelopment));
            });

        builder.AddOpenTelemetryExporters();
        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        var appInsightsConnectionString = ApplicationInsightsConnectionStringResolver.Resolve(builder.Configuration);
        if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
        {
            builder.Services.AddOpenTelemetry().UseAzureMonitor(options =>
                options.ConnectionString = appInsightsConnectionString);
        }

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var healthChecks = builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live", "ready"]);

        var connectionString = DatabaseConnectionStringResolver.Resolve(builder.Configuration);

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            healthChecks.AddSqlServer(connectionString, name: "sql", tags: ["ready"]);
        }

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks(HealthEndpointPath, new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
        }).AllowAnonymous();

        app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
        }).AllowAnonymous();

        return app;
    }

    private static bool CommandContainsPhi(object command)
    {
        var text = command switch
        {
            SqlCommand sql => sql.CommandText,
            System.Data.Common.DbCommand db => db.CommandText,
            _ => command.ToString(),
        };

        return !string.IsNullOrEmpty(text)
            && (text.Contains("MedicareNumber", StringComparison.OrdinalIgnoreCase)
                || text.Contains("MedicareNumberBlindIndex", StringComparison.OrdinalIgnoreCase)
                || text.Contains("MedicareBlindIndex", StringComparison.OrdinalIgnoreCase)
                || text.Contains("DateOfBirth", StringComparison.OrdinalIgnoreCase)
                || text.Contains("MedicarePartAEffectiveDate", StringComparison.OrdinalIgnoreCase)
                || text.Contains("MedicarePartBEffectiveDate", StringComparison.OrdinalIgnoreCase));
    }
}
