using System.Threading.RateLimiting;
using AppHost.Authorization;
using AppHost.Configuration;
using AppHost.Health;
using Backoffice.Api;
using BuildingBlocks.Application.Authorization;
using BuildingBlocks.Application.Diagnostics;
using BuildingBlocks.Application.Extensions;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Infrastructure.Extensions;
using BuildingBlocks.Infrastructure.Messaging;
using BuildingBlocks.Infrastructure.Modules;
using BuildingBlocks.Infrastructure.Operations;
using BuildingBlocks.Infrastructure.Persistence;
using Cart.Api;
using Catalog.Api;
using Customers.Api;
using FluentValidation;
using Inventory.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Orders.Api;
using Payments.Api;
using Pricing.Api;
using Redirects.Api;
using Reviews.Api;
using Search.Api;
using Serilog;
using Shipping.Api;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "AppHost")
        .WriteTo.Console();
});

builder.Services.AddOptions<AppObservabilityOptions>()
    .BindConfiguration(AppObservabilityOptions.SectionName)
    .Validate(options => !string.IsNullOrWhiteSpace(options.ServiceName), "Observability service name is required.")
    .ValidateOnStart();
builder.Services.AddOptions<AppRateLimitingOptions>()
    .BindConfiguration(AppRateLimitingOptions.SectionName)
    .ValidateOnStart();
builder.Services.AddOptions<AppSecurityOptions>()
    .BindConfiguration(AppSecurityOptions.SectionName)
    .ValidateOnStart();
builder.Services.AddOptions<AppReadinessOptions>()
    .BindConfiguration(AppReadinessOptions.SectionName)
    .ValidateOnStart();
builder.Services.AddOptions<AppBuildOptions>()
    .BindConfiguration(AppBuildOptions.SectionName)
    .ValidateOnStart();
builder.Services.AddOptions<AppReleaseOptions>()
    .BindConfiguration(AppReleaseOptions.SectionName)
    .Validate(options => ReleaseSeedModes.IsSupported(options.SeedMode), "Release seed mode is invalid.")
    .ValidateOnStart();
builder.Services.AddOptions<AppFeatureFlagsOptions>()
    .BindConfiguration(AppFeatureFlagsOptions.SectionName)
    .ValidateOnStart();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;
        context.ProblemDetails.Extensions["traceId"] = System.Diagnostics.Activity.Current?.TraceId.ToString();
    };
});
builder.Services.AddApplicationCore();
builder.Services.AddSharedInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, BackofficeAuthorizationPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, BackofficeAuthorizationHandler>();

var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!RedisConnectionHelper.CanConnect(redisConnectionString))
{
    builder.Services.AddDistributedMemoryCache();
}
else
{
    builder.Services.AddStackExchangeRedisCache(options =>
        options.ConfigurationOptions = RedisConnectionHelper.BuildOptions(redisConnectionString!));
}

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
        options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
    })
    .AddCookie(IdentityConstants.ApplicationScheme, options =>
    {
        options.Cookie.Name = "blazor-ecommerce-auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing")
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
            {
                return ApiProblemDetailsWriter.WriteUnauthorizedAsync(context.HttpContext);
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
            {
                return ApiProblemDetailsWriter.WriteForbiddenAsync(context.HttpContext);
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    })
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Authentication:Jwt:Authority"];
        options.Audience = builder.Configuration["Authentication:Jwt:Audience"];
        options.RequireHttpsMetadata = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    var rateLimitOptions = builder.Configuration.GetSection(AppRateLimitingOptions.SectionName).Get<AppRateLimitingOptions>() ?? new AppRateLimitingOptions();
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";
        return new ValueTask(context.HttpContext.Response.WriteAsJsonAsync(
            new
            {
                title = "Too many requests",
                status = StatusCodes.Status429TooManyRequests,
                detail = "Request rate limit exceeded.",
                correlationId = context.HttpContext.TraceIdentifier,
            },
            cancellationToken));
    };

    options.AddPolicy(RateLimitingPolicyNames.Auth, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetPartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = Math.Max(1, rateLimitOptions.AuthPermits),
                Window = TimeSpan.FromSeconds(Math.Max(1, rateLimitOptions.AuthWindowSeconds)),
                QueueLimit = 0,
            }));

    options.AddPolicy(RateLimitingPolicyNames.ReviewsWrite, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetPartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = Math.Max(1, rateLimitOptions.ReviewPermits),
                Window = TimeSpan.FromSeconds(Math.Max(1, rateLimitOptions.ReviewWindowSeconds)),
                QueueLimit = 0,
            }));

    options.AddPolicy(RateLimitingPolicyNames.SearchSuggest, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetPartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = Math.Max(1, rateLimitOptions.SearchSuggestPermits),
                Window = TimeSpan.FromSeconds(Math.Max(1, rateLimitOptions.SearchSuggestWindowSeconds)),
                QueueLimit = 0,
            }));

    options.AddPolicy(RateLimitingPolicyNames.PaymentMutations, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetPartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = Math.Max(1, rateLimitOptions.PaymentPermits),
                Window = TimeSpan.FromSeconds(Math.Max(1, rateLimitOptions.PaymentWindowSeconds)),
                QueueLimit = 0,
            }));

    options.AddPolicy(RateLimitingPolicyNames.PublicWebhook, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetPartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = Math.Max(1, rateLimitOptions.WebhookPermits),
                Window = TimeSpan.FromSeconds(Math.Max(1, rateLimitOptions.WebhookWindowSeconds)),
                QueueLimit = 0,
            }));
});

var moduleInstallers = ModuleInfrastructureLoader.LoadInstallers();
foreach (var installer in moduleInstallers)
{
    installer.AddInfrastructure(builder.Services, builder.Configuration);
}

builder.Services.AddCatalogModule();
builder.Services.AddBackofficeModule();
builder.Services.AddCartModule();
builder.Services.AddOrdersModule();
builder.Services.AddRedirectsModule();
builder.Services.AddSearchModule();
builder.Services.AddCustomersModule();
builder.Services.AddInventoryModule();
builder.Services.AddPaymentsModule();
builder.Services.AddPricingModule();
builder.Services.AddReviewsModule();
builder.Services.AddShippingModule();

builder.Services.AddHealthChecks()
    .AddCheck("process", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live", "startup"])
    .AddDbContextCheck<OutboxDbContext>("postgres", tags: ["ready", "startup"])
    .AddCheck<RedisReadinessHealthCheck>("redis", failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded, tags: ["ready"])
    .AddCheck<OutboxReadinessHealthCheck>("outbox", failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded, tags: ["ready"]);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
    {
        var observability = builder.Configuration.GetSection(AppObservabilityOptions.SectionName).Get<AppObservabilityOptions>() ?? new AppObservabilityOptions();
        resource.AddAttributes(
        [
            new KeyValuePair<string, object>("service.name", observability.ServiceName),
        ]);
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddSource(CommerceDiagnostics.ActivitySourceName);

        var observability = builder.Configuration.GetSection(AppObservabilityOptions.SectionName).Get<AppObservabilityOptions>() ?? new AppObservabilityOptions();
        if (observability.EnableConsoleExporter)
        {
            tracing.AddConsoleExporter();
        }
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter(CommerceDiagnostics.MeterName);

        var observability = builder.Configuration.GetSection(AppObservabilityOptions.SectionName).Get<AppObservabilityOptions>() ?? new AppObservabilityOptions();
        if (observability.EnableConsoleExporter)
        {
            metrics.AddConsoleExporter();
        }
    });

var app = builder.Build();
var skipInfrastructureInitialization = builder.Configuration.GetValue<bool>("Infrastructure:SkipInitialization");
var buildOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<AppBuildOptions>>().Value;
var releaseOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<AppReleaseOptions>>().Value;

app.Logger.LogInformation(
    "Starting app host {ApplicationName} version {Version} revision {Revision}",
    buildOptions.ApplicationName,
    buildOptions.Version,
    buildOptions.SourceRevisionId ?? "n/a");

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseExceptionHandler();
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
        diagnosticContext.Set("TraceId", System.Diagnostics.Activity.Current?.TraceId.ToString());
        diagnosticContext.Set("UserId", httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
    };
});
app.Use(async (context, next) =>
{
    var security = context.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<AppSecurityOptions>>().Value;
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", security.FrameOptions);
    context.Response.Headers.TryAdd("Referrer-Policy", security.ReferrerPolicy);
    if (!string.IsNullOrWhiteSpace(security.ContentSecurityPolicy))
    {
        context.Response.Headers.TryAdd("Content-Security-Policy", security.ContentSecurityPolicy);
    }

    await next();
});
app.UseRateLimiter();
app.UseRedirectRules();
app.UseAuthentication();
app.UseAuthorization();

if (!skipInfrastructureInitialization)
{
    await app.Services.InitializeSharedInfrastructureAsync(app.Lifetime.ApplicationStopping);
    await moduleInstallers.InitializeModulesAsync(app.Services, app.Lifetime.ApplicationStopping);
    await moduleInstallers.SeedModulesAsync(app.Services, releaseOptions.SeedMode, app.Lifetime.ApplicationStopping);
}

var apiV1 = app.MapGroup("/api/v1");
apiV1.MapCatalogEndpoints();
apiV1.MapBackofficeEndpoints();
apiV1.MapCartEndpoints();
apiV1.MapOrdersEndpoints();
apiV1.MapRedirectEndpoints();
apiV1.MapSearchEndpoints();
apiV1.MapCustomersEndpoints();
apiV1.MapInventoryEndpoints();
apiV1.MapPaymentsEndpoints();
apiV1.MapPricingEndpoints();
apiV1.MapReviewsEndpoints();
apiV1.MapShippingEndpoints();
app.MapDirectusWebhookEndpoint();
app.MapGet("/version", (
    IWebHostEnvironment environment,
    Microsoft.Extensions.Options.IOptions<AppBuildOptions> build,
    Microsoft.Extensions.Options.IOptions<AppReleaseOptions> release,
    Microsoft.Extensions.Options.IOptions<AppFeatureFlagsOptions> featureFlags) =>
{
    return Results.Ok(new
    {
        application = build.Value.ApplicationName,
        version = build.Value.Version,
        revision = build.Value.SourceRevisionId,
        buildTimestampUtc = build.Value.BuildTimestampUtc,
        environment = environment.EnvironmentName,
        release = new
        {
            release.Value.SeedMode,
            release.Value.MigrationMode,
            release.Value.RunSmokeTestsAfterDeploy,
        },
        activeFeatureFlags = GetActiveFeatureFlags(featureFlags.Value),
    });
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live"),
    ResponseWriter = HealthResponseWriter.WriteAsync,
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = HealthResponseWriter.WriteAsync,
});

app.MapHealthChecks("/health/startup", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("startup"),
    ResponseWriter = HealthResponseWriter.WriteAsync,
});

static string GetPartitionKey(HttpContext httpContext)
{
    var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!string.IsNullOrWhiteSpace(userId))
    {
        return $"user:{userId}";
    }

    return $"ip:{httpContext.Connection.RemoteIpAddress}";
}

static IReadOnlyCollection<string> GetActiveFeatureFlags(AppFeatureFlagsOptions options)
{
    var activeFlags = new List<string>();
    if (options.EnableOperationalRecoveryActions)
    {
        activeFlags.Add(nameof(AppFeatureFlagsOptions.EnableOperationalRecoveryActions));
    }

    if (options.EnableDemoProviders)
    {
        activeFlags.Add(nameof(AppFeatureFlagsOptions.EnableDemoProviders));
    }

    if (options.EnableReviewModeration)
    {
        activeFlags.Add(nameof(AppFeatureFlagsOptions.EnableReviewModeration));
    }

    return activeFlags;
}

app.Run();

public partial class Program;
