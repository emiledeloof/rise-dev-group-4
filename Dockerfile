# Production image of Rise.Server (API + Blazor WASM client).
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

# Copy source and publish
COPY src/ src/
# Publishing the server also publishes the referenced Blazor client into wwwroot.
RUN dotnet publish src/Rise.Server/Rise.Server.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    UseHttpsRedirection=false
EXPOSE 8080

COPY --from=build /app/publish .

# Serilog file sink writes to /app/Logs, app runs under unprivileged $APP_UID
RUN mkdir -p /app/Logs && chown $APP_UID /app/Logs
USER $APP_UID

ENTRYPOINT ["dotnet", "Rise.Server.dll"]