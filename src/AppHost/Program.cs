using BuildingBlocks.Application.Extensions;
using BuildingBlocks.Infrastructure.Extensions;
using BuildingBlocks.Infrastructure.Modules;
using BuildingBlocks.Infrastructure.Persistence;
using Cart.Api;
using Catalog.Api;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Trace;
using Orders.Api;
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

builder.Services.AddProblemDetails();
builder.Services.AddApplicationCore();
builder.Services.AddSharedInfrastructure(builder.Configuration);

var moduleInstallers = ModuleInfrastructureLoader.LoadInstallers();
foreach (var installer in moduleInstallers)
{
    installer.AddInfrastructure(builder.Services, builder.Configuration);
}

builder.Services.AddCatalogModule();
builder.Services.AddCartModule();
builder.Services.AddOrdersModule();

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

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

await app.Services.InitializeSharedInfrastructureAsync(app.Lifetime.ApplicationStopping);
await moduleInstallers.InitializeModulesAsync(app.Services, app.Lifetime.ApplicationStopping);

var apiV1 = app.MapGroup("/api/v1");
apiV1.MapCatalogEndpoints();
apiV1.MapCartEndpoints();
apiV1.MapOrdersEndpoints();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

app.Run();
