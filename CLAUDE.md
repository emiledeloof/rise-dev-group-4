# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

Requires the .NET 10 SDK. Solution file: `Rise.sln`.

```bash
dotnet build Rise.sln
dotnet test Rise.sln                                                     # all tests
dotnet test tests/Rise.Services.Tests                                    # one project
dotnet test --filter "FullyQualifiedName~ProjectServiceShould.EditProjectWhenTechnicianIsTheSame"  # one test

# Run the app (from src/Rise.Server) — serves API + Blazor client on https://localhost:5001, Swagger at /swagger
dotnet watch --non-interactive
dotnet run

# EF Core migrations (from src/, needs `dotnet tool install --global dotnet-ef`)
dotnet ef migrations add <Name> --startup-project Rise.Server --project Rise.Persistence
dotnet ef database update --startup-project Rise.Server --project Rise.Persistence
```

In Development, the server applies migrations and runs `DbSeeder` on startup. The default database is SQLite (`Rise.db`). Seeded users (`user@`, `technician1@`, `technician2@`, `secretary@`, `admin@example.com`) all use the password `A1b2C3!`. The roles are Technician, Secretary and Administrator.

## Architecture

This template combines Clean Architecture, DDD and vertical slices. The project dependencies are:

- `Rise.Domain`: entities only. It has no EF or ASP.NET references and depends only on GuardClauses, Ardalis.Result and Serilog.
- `Rise.Shared`: contracts used by both the client and the server. These are service interfaces (`IProductService`), request/response/DTO classes and FluentValidation validators. It never references Domain.
- `Rise.Persistence` → Domain: `ApplicationDbContext` (an `IdentityDbContext`), entity configurations, migrations and triggers.
- `Rise.Services` → Domain, Persistence, Shared: implementations of the Shared service interfaces. They use `ApplicationDbContext` directly; there is deliberately no repository abstraction.
- `Rise.Server` → everything, including Client: FastEndpoints host and composition root. It also serves the Blazor WASM client through `UseBlazorFrameworkFiles` + `MapFallbackToFile("index.html")`.
- `Rise.Client` → Shared only: Blazor WebAssembly. It has its **own** implementations of the Shared interfaces (e.g. `Client/Products/ProductService.cs`), which call the API over `HttpClient`.

### Adding a feature (follow the Products slice)

1. **Shared** `<Feature>/`: put the request types in a `static partial class <Feature>Request` with nested classes (`Create`, `Edit`, …), each with a nested `Validator : AbstractValidator<>`. Add the matching `<Feature>Response` partial class, `<Feature>Dto` nested DTOs and the `I<Feature>Service` interface returning `Task<Result<T>>`. Use `QueryRequest.SkipTake`/`Cursor<T>` for list queries; their `ToString()` builds the query string.
2. **Domain**: create an entity that inherits `Entity` (int `Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted`; equality is by Id). Enforce invariants in property setters/constructors with `Guard.Against`. Add a private parameterless ctor for EF.
3. **Persistence**: create a configuration that inherits `EntityConfiguration<T>` and calls `base.Configure`; it is picked up automatically via `ApplyConfigurationsFromAssembly`. Add a `DbSet` to `ApplicationDbContext`, then add a migration. Tables use the singular class name, strings default to max length 4000, and decimals default to (18,2).
4. **Services**: implement the interface and register it in `ServiceCollectionExtensions.AddApplicationServices`.
5. **Server** `Endpoints/<Feature>/`: one class per endpoint, `Endpoint<TRequest, Result<TResponse>>`, which delegates to the service. Use `Roles(AppRoles.X)` or `AllowAnonymous()` in `Configure`.
6. **Client**: write the `HttpClient` implementation of the interface, register it in `Client/Program.cs`, and add the Razor pages (code-behind in `.razor.cs`).

### Cross-cutting conventions

- **Responses**: endpoints return `Ardalis.Result`, and auto-send is disabled (`ep.DontAutoSendResponse()`). `Processors/GlobalResponseSender` maps `ResultStatus` to HTTP codes (Invalid→400, NotFound→404, Conflict→409, Error→422, …). Return the right `Result.*` from services instead of throwing or setting status codes. The client deserializes the body as `Result<T>`.
- **Validation**: FastEndpoints discovers validators from the `Rise.Shared` assembly (configured in `Server/Program.cs`). The client uses the same validators via Blazored.FluentValidation.
- **Timestamps / soft delete**: `Persistence/Triggers/EntityBeforeSaveTrigger` sets `CreatedAt`/`UpdatedAt`. It also converts deletes into `IsDeleted = true` updates, so never set these fields manually. Note that no global query filter on `IsDeleted` exists yet.
- **Current user in services**: inject `ISessionContextProvider` (defined in `Services/Identity/IIdentityContextProvider.cs`, implemented by `HttpContextSessionProvider` in Server). Use `User.GetUserId()` from `Shared/Identity/ClaimsPrincipalExtentions.cs`. Tests substitute `FakeSessionContextProvider`. The file names differ from the type names.
- **Auth**: ASP.NET Identity with cookie auth. The client uses `CookieAuthenticationStateProvider` (which also implements `IAccountManager`) and `CookieHandler` on the `"SecureApi"` HttpClient. Role names live in `Shared/Identity/Roles.cs` (`AppRoles`), and policies live in `Policies.cs`.
- **Logging**: Serilog is configured in `appsettings.json` (console + `Logs/` file). Mark sensitive DTO properties with Destructurama attributes so they are masked.
- **DB-provider-specific bits**: if the provider is swapped, update `UseSqlite` in `Server/Program.cs` and `HasDefaultValueSql("CURRENT_TIMESTAMP")` in `EntityConfiguration`.

## Tests

xUnit + Shouldly everywhere; NSubstitute is available.
- Tests are named `<Subject>Should` and methods describe behavior, e.g. `EditProjectWhenTechnicianIsTheSame`.
- `Rise.Client.Tests`: bUnit. Test classes inherit `BunitContext`, register fakes (e.g. `FakeProductService`) and use `this.AddAuthorization().SetAuthorized(...)`. Their root namespace is `Rise.Client`.
- `Rise.Services.Tests`: these currently use the EF InMemory provider, though a comment in the code advises moving to a real database with cleanup between tests.
- There are no integration tests yet. The README points to FastEndpoints integration testing.
