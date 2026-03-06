using BuildingBlocks.Application.Extensions;
using BuildingBlocks.Infrastructure.Extensions;
using BuildingBlocks.Infrastructure.Modules;
using BuildingBlocks.Infrastructure.Persistence;
using Cart.Api;
using Catalog.Api;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Trace;
using Orders.Api;
using Redirects.Api;
using Search.Api;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddApplicationCore();
builder.Services.AddSharedInfrastructure(builder.Configuration);

var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddDistributedMemoryCache();
}
else
{
    builder.Services.AddStackExchangeRedisCache(options => options.Configuration = redisConnectionString);
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Authentication:Jwt:Authority"];
        options.Audience = builder.Configuration["Authentication:Jwt:Audience"];
        options.RequireHttpsMetadata = true;
    });

builder.Services.AddAuthorization();

var moduleInstallers = ModuleInfrastructureLoader.LoadInstallers();
foreach (var installer in moduleInstallers)
{
    installer.AddInfrastructure(builder.Services, builder.Configuration);
}

builder.Services.AddCatalogModule();
builder.Services.AddCartModule();
builder.Services.AddOrdersModule();
builder.Services.AddRedirectsModule();
builder.Services.AddSearchModule();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<OutboxDbContext>("postgres", tags: ["ready"]);

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddConsoleExporter();
    });

var app = builder.Build();
var skipInfrastructureInitialization = builder.Configuration.GetValue<bool>("Infrastructure:SkipInitialization");

app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseRedirectRules();
app.UseAuthentication();
app.UseAuthorization();

if (!skipInfrastructureInitialization)
{
    await app.Services.InitializeSharedInfrastructureAsync(app.Lifetime.ApplicationStopping);
    await moduleInstallers.InitializeModulesAsync(app.Services, app.Lifetime.ApplicationStopping);
}

var apiV1 = app.MapGroup("/api/v1");
apiV1.MapCatalogEndpoints();
apiV1.MapCartEndpoints();
apiV1.MapOrdersEndpoints();
apiV1.MapRedirectEndpoints();
apiV1.MapSearchEndpoints();
app.MapDirectusWebhookEndpoint();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
});

app.Run();

public partial class Program;
