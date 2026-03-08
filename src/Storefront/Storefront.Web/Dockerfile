FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore src/Storefront/Storefront.Web/Storefront.Web.csproj
RUN dotnet publish src/Storefront/Storefront.Web/Storefront.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .

ENV ASPNETCORE_HTTP_PORTS=5100
EXPOSE 5100

USER $APP_UID

ENTRYPOINT ["dotnet", "Storefront.Web.dll"]
