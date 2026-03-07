using AppHost.Authorization;
using Backoffice.Api;
using BuildingBlocks.Application.Authorization;
using BuildingBlocks.Application.Extensions;
using BuildingBlocks.Infrastructure.Extensions;
using BuildingBlocks.Infrastructure.Modules;
using BuildingBlocks.Infrastructure.Persistence;
using Cart.Api;
using Catalog.Api;
using Customers.Api;
using FluentValidation;
using Inventory.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        .WriteTo.Console();
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddApplicationCore();
builder.Services.AddSharedInfrastructure(builder.Configuration);
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
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
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
