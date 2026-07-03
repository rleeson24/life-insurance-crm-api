using LifeInsuranceCRM.API.Auth;
using LifeInsuranceCRM.API.ExceptionHandling;
using LifeInsuranceCRM.API.Middleware;
using LifeInsuranceCRM.API.RateLimiting;
using LifeInsuranceCRM.API.Services;
using LifeInsuranceCRM.Core;
using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Config;
using LifeInsuranceCRM.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

AddConfigurationOptions(builder);
AddWebApi(builder);
AddDevelopmentCors(builder);
AddAuthentication(builder);
AddRateLimitingPolicies(builder);
builder.Services.AddDataServices();
builder.Services.AddCoreServices();

var app = builder.Build();

MapHealthEndpoints(app);
MapAspireDefaults(app);
UseDevelopmentOpenApi(app);
UseGlobalExceptionHandler(app);
UseDevelopmentCors(app);
UseHttps(app);
UseRateLimiter(app);
UseAuthentication(app);
UseAuthorization(app);
UseActorResolution(app);
UseAuthChallengeRecording(app);
MapControllers(app);

app.Run();

void AddConfigurationOptions(WebApplicationBuilder webBuilder)
{
    webBuilder.Services.Configure<AuthOptions>(webBuilder.Configuration.GetSection(AuthOptions.SectionName));
    webBuilder.Services.Configure<RateLimitingOptions>(webBuilder.Configuration.GetSection(RateLimitingOptions.SectionName));
    webBuilder.Services.Configure<DatabaseOptions>(options =>
    {
        webBuilder.Configuration.GetSection(DatabaseOptions.SectionName).Bind(options);

        // Aspire injects ConnectionStrings:LifeInsuranceCRM when running via AppHost.
        var aspireConnectionString = webBuilder.Configuration.GetConnectionString("LifeInsuranceCRM");
        if (!string.IsNullOrWhiteSpace(aspireConnectionString))
        {
            options.ConnectionString = aspireConnectionString;
        }
    });
}

void AddWebApi(WebApplicationBuilder webBuilder)
{
    webBuilder.Services.AddHttpContextAccessor();
    webBuilder.Services.AddScoped<IActorTracker, ActorTracker>();
    webBuilder.Services.AddSingleton<IProblemDetailsFactory, ProblemDetailsFactory>();
    webBuilder.Services.AddScoped<IProcessResponseActionMapper, ProcessResponseActionMapper>();
    webBuilder.Services.AddControllers();
    webBuilder.Services.AddOpenApi();
    webBuilder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    webBuilder.Services.AddProblemDetails();
}

void AddAuthentication(WebApplicationBuilder webBuilder)
{
    var authOptions = webBuilder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();

    var authenticationBuilder = webBuilder.Services.AddAuthentication();

    if (authOptions.UseDevelopmentAuthentication)
    {
        authenticationBuilder
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
                DevelopmentAuthenticationDefaults.Scheme,
                _ => { });

        webBuilder.Services.AddAuthorization();
        webBuilder.Services.PostConfigure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(options =>
        {
            options.DefaultAuthenticateScheme = DevelopmentAuthenticationDefaults.Scheme;
            options.DefaultChallengeScheme = DevelopmentAuthenticationDefaults.Scheme;
        });
    }
    else
    {
        webBuilder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(webBuilder.Configuration.GetSection("AzureAd"));
        webBuilder.Services.AddAuthorization();
    }
}

void AddDevelopmentCors(WebApplicationBuilder webBuilder)
{
    if (!webBuilder.Environment.IsDevelopment())
    {
        return;
    }

    webBuilder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.SetIsOriginAllowed(_ => true)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());
    });
}

void UseDevelopmentCors(WebApplication webApp)
{
    if (webApp.Environment.IsDevelopment())
    {
        webApp.UseCors();
    }
}

void AddRateLimitingPolicies(WebApplicationBuilder webBuilder)
{
    var rateOptions = webBuilder.Configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
        ?? new RateLimitingOptions();
    webBuilder.Services.AddCrmRateLimiting(rateOptions);
}

void MapHealthEndpoints(WebApplication webApp) => webApp.MapDefaultEndpoints();

void MapAspireDefaults(WebApplication webApp)
{
    // Reserved for additional Aspire-specific endpoints beyond health checks.
}

void UseDevelopmentOpenApi(WebApplication webApp)
{
    if (webApp.Environment.IsDevelopment())
    {
        webApp.MapOpenApi();
    }
}

void UseGlobalExceptionHandler(WebApplication webApp) => webApp.UseExceptionHandler();

void UseHttps(WebApplication webApp) => webApp.UseHttpsRedirection();

void UseRateLimiter(WebApplication webApp) => webApp.UseRateLimiter();

void UseAuthentication(WebApplication webApp) => webApp.UseAuthentication();

void UseAuthorization(WebApplication webApp) => webApp.UseAuthorization();

void UseActorResolution(WebApplication webApp) =>
    webApp.UseMiddleware<ActorResolutionMiddleware>();

void UseAuthChallengeRecording(WebApplication webApp) =>
    webApp.UseMiddleware<AuthChallengeRecordingMiddleware>();

void MapControllers(WebApplication webApp) => webApp.MapControllers();
