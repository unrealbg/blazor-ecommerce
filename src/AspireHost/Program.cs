SetDefaultEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
SetDefaultEnvironmentVariable("DOTNET_ENVIRONMENT", "Development");
SetDefaultEnvironmentVariable("ASPNETCORE_URLS", "https://localhost:15000");
SetDefaultEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");
SetDefaultEnvironmentVariable("ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL", "https://localhost:16000");
SetDefaultEnvironmentVariable("ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL", "https://localhost:16001");
SetDefaultEnvironmentVariable("ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL", "https://localhost:16002");

var builder = DistributedApplication.CreateBuilder(args);

var postgresPassword = builder.AddParameter("postgres-password", secret: true);
var directusDatabaseUser = builder.AddParameter("directus-db-username");
var directusDatabasePassword = builder.AddParameter("directus-db-password", secret: true);
var directusKey = builder.AddParameter("directus-key", secret: true);
var directusSecret = builder.AddParameter("directus-secret", secret: true);
var directusAdminEmail = builder.AddParameter("directus-admin-email");
var directusAdminPassword = builder.AddParameter("directus-admin-password", secret: true);

var postgres = builder
    .AddPostgres("postgres", password: postgresPassword)
    .WithDataVolume("blazor-ecommerce-postgres");

var commerceDatabase = postgres.AddDatabase("commerce-db", "blazor_ecommerce");

var directusPostgres = builder
    .AddPostgres("directus-postgres", userName: directusDatabaseUser, password: directusDatabasePassword)
    .WithDataVolume("blazor-ecommerce-directus-postgres");

var directusDatabase = directusPostgres.AddDatabase("directus-db", "directus");

var redis = builder
    .AddRedis("redis")
    .WithDataVolume("blazor-ecommerce-redis");

var directus = builder
    .AddContainer("directus", "directus/directus", "11.14.0")
    .WithHttpEndpoint(port: 8055, targetPort: 8055, name: "http")
    .WithVolume("blazor-ecommerce-directus-uploads", "/directus/uploads")
    .WithEnvironment("KEY", directusKey)
    .WithEnvironment("SECRET", directusSecret)
    .WithEnvironment("ADMIN_EMAIL", directusAdminEmail)
    .WithEnvironment("ADMIN_PASSWORD", directusAdminPassword)
    .WithEnvironment("DB_CLIENT", "pg")
    .WithEnvironment("DB_HOST", directusPostgres.Resource.Host)
    .WithEnvironment("DB_PORT", directusPostgres.Resource.Port)
    .WithEnvironment("DB_DATABASE", directusDatabase.Resource.DatabaseName)
    .WithEnvironment("DB_USER", directusPostgres.Resource.UserNameReference)
    .WithEnvironment("DB_PASSWORD", directusDatabasePassword)
    .WithEnvironment("WEBSOCKETS_ENABLED", "true")
    .WithReference(directusDatabase)
    .WaitFor(directusDatabase);

var backendApi = builder
    .AddProject<Projects.AppHost>("backend-api")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("ConnectionStrings__Postgres", commerceDatabase)
    .WithEnvironment("ConnectionStrings__Redis", redis)
    .WithReference(commerceDatabase)
    .WithReference(redis)
    .WaitFor(commerceDatabase)
    .WaitFor(redis);

builder
    .AddProject<Projects.Storefront_Web>("storefront-web")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("Api__BaseUrl", backendApi.GetEndpoint("http"))
    .WithReference(backendApi)
    .WaitFor(backendApi);

builder.Build().Run();

static void SetDefaultEnvironmentVariable(string name, string value)
{
    if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)))
    {
        Environment.SetEnvironmentVariable(name, value);
    }
}
