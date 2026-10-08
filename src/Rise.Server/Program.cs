using Destructurama;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rise.Persistence;
using Rise.Persistence.Triggers;
using Rise.Server.Identity;
using Rise.Server.Processors;
using Rise.Services;
using Rise.Services.Identity;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger(); // Initial log setup, will be overwritten by Serilog, but we need a logger before Dependency Injection is activated.

try
{
    Log.Information("Starting web application");
    var builder = WebApplication.CreateBuilder(args);

    builder.Services
        .AddSerilog((_, lc) => lc.ReadFrom.Configuration(builder.Configuration) // Configuration in AppSettings.json
            .Destructure.UsingAttributes()) // Sensitive data logging
        .AddIdentity<IdentityUser, IdentityRole>() 
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .Services.AddDbContext<ApplicationDbContext>(o =>
        {
            // Empty placeholder in appsettings.json, overridden by User Secrets in development (see README)
            // and by the env var ConnectionStrings__DatabaseConnection in other environments.
            var connectionString = builder.Configuration.GetConnectionString("DatabaseConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "Connection string 'DatabaseConnection' not found. In development run (in src/Rise.Server): " +
                    "dotnet user-secrets set \"ConnectionStrings:DatabaseConnection\" " +
                    "\"Host=localhost;Port=5433;Database=rise;Username=rise;Password=<POSTGRES_PASSWORD from .env>\". " +
                    "Other environments: set the env var ConnectionStrings__DatabaseConnection.");
            o.UseNpgsql(connectionString); // PostgreSQL, run it locally with `docker compose up -d` (see docker-compose.yml).
            o.EnableDetailedErrors();
            if (builder.Environment.IsDevelopment())
            {
                o.EnableSensitiveDataLogging(); // only enabled in development.
            }
            o.UseTriggers(options => options.AddTrigger<EntityBeforeSaveTrigger>()); // Handles all UpdatedAt, CreatedAt stuff.
        })
        .AddHttpContextAccessor()
        .AddScoped<ISessionContextProvider, HttpContextSessionProvider>() // Provides the current user from the HttpContext to the session provider.
        .AddApplicationServices() // You'll need to add your own services in this function call.
        .AddAuthorization()
        .AddFastEndpoints(o =>
        {
            o.IncludeAbstractValidators = true; // Include validators from abstract classes (see https://docs.fluentvalidation.net/en/latest/).
            o.Assemblies = [typeof(Rise.Shared.Products.ProductRequest).Assembly]; // Adds the validators from other assemblies
        })
        .SwaggerDocument(o =>
        {
            o.DocumentSettings = s =>
            {
                s.Title = "RISE API";
            };
        });

    var app = builder.Build();
    // apply Database migraticons on startup, not so wise in production (Use Generated SQL Scripts) 
    // See: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying?tabs=dotnet-core-cli
    if (app.Environment.IsDevelopment())
    {
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var dbSeeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
            dbContext.Database.EnsureDeleted(); // Start every development run with a fresh, seeded database.

            dbContext.Database.Migrate(); // Creates the database if it doesn't exist and applies all migrations. See Readme.md for more info.
            await dbSeeder.SeedAsync(); // Seeds the database with some test data.
        }
    }
    else if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
    {
        // Docker deployments (see docker-compose.local.yml): migrate without wiping, seeding is optional (DbSeeder skips existing data).
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();
        if (app.Configuration.GetValue<bool>("Database:Seed"))
            await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync();
    }

    // Behind a reverse proxy (TLS terminated there) the original scheme and client IP come from the X-Forwarded-* headers.
    var forwardedHeaders = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    };
    forwardedHeaders.KnownIPNetworks.Clear(); // trust any proxy, the container port is only reachable through it
    forwardedHeaders.KnownProxies.Clear();
    app.UseForwardedHeaders(forwardedHeaders);
    // The container only listens on HTTP (ASPNETCORE_HTTP_PORTS), HTTPS is handled by the reverse proxy.
    if (app.Configuration.GetValue("UseHttpsRedirection", true))
        app.UseHttpsRedirection();

    // Theses middlewares are strict in order of calling!
    app.UseBlazorFrameworkFiles() // Blazor is also served from the API.
        .UseStaticFiles()
        .UseDefaultExceptionHandler()
        .UseAuthentication()
        .UseAuthorization()
        .UseFastEndpoints(o =>
        {
            o.Endpoints.Configurator = ep =>
            {
                ep.DontAutoSendResponse();
                ep.PreProcessor<GlobalRequestLogger>(Order.Before);
                ep.PostProcessor<GlobalResponseSender>(Order.Before);
                ep.PostProcessor<GlobalResponseLogger>(Order.Before);
            };
        })
        .UseSwaggerGen();
    app.MapFallbackToFile("index.html"); // Serves the Blazor app from the API, when no routes match.
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "An unhandled exception occured during bootstrapping");
}
finally
{
    Log.CloseAndFlush();
}


