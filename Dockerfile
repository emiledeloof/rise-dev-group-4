# Production image of Rise.Server (API + Blazor WASM client).
# Local test deployment: `docker compose -f docker-compose.local.yml up -d --build` (see README).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, so the package layer is cached as long as no project file changes.
COPY src/Rise.Domain/Rise.Domain.csproj           src/Rise.Domain/
COPY src/Rise.Shared/Rise.Shared.csproj           src/Rise.Shared/
COPY src/Rise.Persistence/Rise.Persistence.csproj src/Rise.Persistence/
COPY src/Rise.Services/Rise.Services.csproj       src/Rise.Services/
COPY src/Rise.Client/Rise.Client.csproj           src/Rise.Client/
COPY src/Rise.Server/Rise.Server.csproj           src/Rise.Server/
RUN dotnet restore src/Rise.Server/Rise.Server.csproj

COPY src/ src/
# Publishing the server also publishes the referenced Blazor client into wwwroot.
RUN dotnet publish src/Rise.Server/Rise.Server.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    UseHttpsRedirection=false
EXPOSE 8080

COPY --from=build /app/publish .
# Serilog's file sink writes to /app/Logs, the app runs as the image's non-root user.
RUN mkdir -p /app/Logs && chown $APP_UID /app/Logs
USER $APP_UID

ENTRYPOINT ["dotnet", "Rise.Server.dll"]
