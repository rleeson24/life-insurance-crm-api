using LifeInsuranceCRM.API.Auth;
using LifeInsuranceCRM.API.Database;
using LifeInsuranceCRM.API.ExceptionHandling;
using LifeInsuranceCRM.API.Middleware;
using LifeInsuranceCRM.API.RateLimiting;
using LifeInsuranceCRM.API.Services;
using LifeInsuranceCRM.Core;
using LifeInsuranceCRM.Core.Abstractions.Auth;
using LifeInsuranceCRM.Core.Config;
using LifeInsuranceCRM.Core.Constants;
using LifeInsuranceCRM.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

AddConfigurationOptions(builder);
AddWebApi(builder);
AddCors(builder);
AddAuthentication(builder);
AddRateLimitingPolicies(builder);
builder.Services.AddDataServices();
builder.Services.AddCoreServices();
builder.Services.AddScoped<IDevelopmentDatabaseInitializer, DevelopmentDatabaseInitializer>();

var app = builder.Build();

await InitializeDevelopmentDatabaseAsync(app);

MapHealthEndpoints(app);
MapAspireDefaults(app);
UseDevelopmentOpenApi(app);
UseGlobalExceptionHandler(app);
UseSecurityHeaders(app);
UseCors(app);
UseHttps(app);
UseRateLimiter(app);
UseAuthentication(app);
UseActorResolution(app);
UseAuthorization(app);
UseAuthChallengeRecording(app);
MapControllers(app);

app.Run();

void AddConfigurationOptions(WebApplicationBuilder webBuilder)
{
    webBuilder.Services.Configure<AuthOptions>(webBuilder.Configuration.GetSection(AuthOptions.SectionName));
    webBuilder.Services.Configure<CorsOptions>(webBuilder.Configuration.GetSection(CorsOptions.SectionName));
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
    webBuilder.Services.AddScoped<IAuthorizationHandler, RoleAuthorizationHandler>();
    webBuilder.Services.AddControllers()
        .ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var factory = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsFactory>();
                var firstError = context.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message))
                    ?? "The request body is invalid.";

                var problem = factory.Create(
                    context.HttpContext,
                    StatusCodes.Status400BadRequest,
                    "Invalid request",
                    firstError,
                    "request.invalid");

                return problem.ToObjectResult();
            };
        });
    webBuilder.Services.AddOpenApi();
    webBuilder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    webBuilder.Services.AddProblemDetails();
}

void AddAuthentication(WebApplicationBuilder webBuilder)
{
    var authOptions = webBuilder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();

    if (webBuilder.Environment.IsProduction() && authOptions.UseDevelopmentAuthentication)
    {
        throw new InvalidOperationException("UseDevelopmentAuthentication must be false in Production.");
    }

    var authenticationBuilder = webBuilder.Services.AddAuthentication();

    if (authOptions.UseDevelopmentAuthentication)
    {
        authenticationBuilder
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
                DevelopmentAuthenticationDefaults.Scheme,
                _ => { });

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
    }

    webBuilder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(
            AuthorizationPolicies.CanRead,
            policy => policy.AddRequirements(new RoleRequirement(
                OrganizationRoles.Admin,
                OrganizationRoles.Agent,
                OrganizationRoles.ReadOnly)));
        options.AddPolicy(
            AuthorizationPolicies.CanWrite,
            policy => policy.AddRequirements(new RoleRequirement(
                OrganizationRoles.Admin,
                OrganizationRoles.Agent)));
        options.AddPolicy(
            AuthorizationPolicies.CanDelete,
            policy => policy.AddRequirements(new RoleRequirement(OrganizationRoles.Admin)));
    });
}

void AddCors(WebApplicationBuilder webBuilder)
{
    if (webBuilder.Environment.IsDevelopment())
    {
        webBuilder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
                policy.SetIsOriginAllowed(_ => true)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
        });
        return;
    }

    var corsOptions = webBuilder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();
    webBuilder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(corsOptions.AllowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());
    });
}

void UseCors(WebApplication webApp) => webApp.UseCors();

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

void UseSecurityHeaders(WebApplication webApp) =>
    webApp.UseMiddleware<SecurityHeadersMiddleware>();

void UseHttps(WebApplication webApp)
{
    webApp.UseHttpsRedirection();
    if (webApp.Environment.IsProduction())
    {
        webApp.UseHsts();
    }
}

void UseRateLimiter(WebApplication webApp) => webApp.UseRateLimiter();

void UseAuthentication(WebApplication webApp) => webApp.UseAuthentication();

void UseAuthorization(WebApplication webApp) => webApp.UseAuthorization();

void UseActorResolution(WebApplication webApp) =>
    webApp.UseMiddleware<ActorResolutionMiddleware>();

void UseAuthChallengeRecording(WebApplication webApp) =>
    webApp.UseMiddleware<AuthChallengeRecordingMiddleware>();

void MapControllers(WebApplication webApp) => webApp.MapControllers();

async Task InitializeDevelopmentDatabaseAsync(WebApplication webApp)
{
    if (!webApp.Environment.IsDevelopment())
    {
        return;
    }

    var connectionString = webApp.Configuration.GetConnectionString("LifeInsuranceCRM")
        ?? webApp.Configuration["Database:ConnectionString"];

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return;
    }

    var initializer = webApp.Services.GetRequiredService<IDevelopmentDatabaseInitializer>();
    await initializer.InitializeAsync(connectionString);
}
