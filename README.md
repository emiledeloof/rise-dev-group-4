# Rise - [GROUPNAME]

## Quick Start

Requires the .NET 10 SDK and [Docker Desktop](https://www.docker.com/products/docker-desktop/) (it must be running). From the repository root:

**One-time setup** (no passwords are committed; each developer chooses their own):

```bash
cp .env.example .env    # then open .env and replace `change-me` with your own password

# store the app's connection string in User Secrets; use the SAME password as in .env
dotnet user-secrets set "ConnectionStrings:DatabaseConnection" "Host=localhost;Port=5433;Database=rise;Username=rise;Password=<your password>" --project src/Rise.Server
```

**Every time:**

```bash
docker compose up -d                    # database only (PostgreSQL on localhost:5433)
docker compose --profile tools up -d    # or: database + pgAdmin at http://localhost:5050
```

Then run `Rise.Server` (https://localhost:5001). See [Installation Instructions](#installation-instructions) for details. To test payments, see [Testing Stripe Payments Locally](#testing-stripe-payments-locally).

> **Coming from the old setup** (password `rise` hard-coded)? After the one-time setup, run `docker compose down -v` once so the database is recreated with your new password.

## Team Members

- [MEMBER1_NAME] - [MEMBER1_EMAIL] - [MEMBER1_GITHUB_USERNAME]

## Technologies & Packages Used

- [Blazor](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor) - Frontend.
- [ASP.NET Core 10](https://dotnet.microsoft.com/en-us/apps/aspnet) - Backend.
- [Entity Framework Core 10](https://learn.microsoft.com/en-us/ef/core/) - Database access.
- [EntityFrameworkCore.Triggered](https://github.com/koenbeuk/EntityFrameworkCore.Triggered) - Database-agnostic Entity Framework Core triggers.
- [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) - Securely store secrets during development.
- [GuardClauses](https://github.com/ardalis/GuardClauses) - Validation helper.
- [Ardalis.Result](https://github.com/ardalis/Result) - Result abstraction that can be mapped to HTTP response codes.
- [FastEndpoints](https://fast-endpoints.com/) - Developer-friendly alternative to Minimal APIs and MVC.
- [Serilog](https://serilog.net/) - Structured logging to the console, files, and other sinks.
- [FluentValidation](https://docs.fluentvalidation.net/en/latest/) - Strongly typed validation rules.
- [Blazored.FluentValidation](https://github.com/Blazored/FluentValidation) - FluentValidation integration for Blazor.
- [bUnit](https://bunit.dev/) - Blazor component testing.
- [xUnit](https://xunit.net/) - Unit testing.
- [NSubstitute](https://nsubstitute.github.io/) - Mocking for testing.
- [Shouldly](https://docs.shouldly.org/) - Assertion library for testing.
- [Destructurama.Attributed](https://github.com/destructurama/attributed) - Masking sensitive data in structured logging.

## Software

1. Install [Rider](https://www.jetbrains.com/rider/), [Visual Studio](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/).
2. Make sure you have the [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) installed.
3. Install [Docker Desktop](https://www.docker.com/products/docker-desktop/) (used to run the PostgreSQL database).

You can verify your installed SDK using:

```bash
dotnet --version
```

## Installation Instructions

1. Clone the repository.

2. Open the `Rise.sln` file in [Rider](https://www.jetbrains.com/rider/), [Visual Studio](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/).

   We prefer Rider, but you're free to choose.

3. Create your local `.env` file in the repository root and choose a database password:

   ```bash
   cp .env.example .env
   ```

   Open `.env` and replace `change-me` with your own password. `.env` is git-ignored, so the password never ends up in the repository. `docker compose` refuses to start without it.

4. Start the PostgreSQL database from the repository root:

   ```bash
   docker compose up -d
   ```

   This starts PostgreSQL 17 on `localhost:5433` (database and user are `rise`, the password comes from `.env`, see `docker-compose.yml`). Port 5433 is used so it does not clash with a locally installed PostgreSQL on 5432.

   Postgres only applies the password when its volume is created for the first time. If you change the password later, recreate the volume with `docker compose down -v` followed by `docker compose up -d`.

5. Store the connection string for the app in [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets). Use the same password as in `.env`, and run this from the repository root:

   ```bash
   dotnet user-secrets set "ConnectionStrings:DatabaseConnection" "Host=localhost;Port=5433;Database=rise;Username=rise;Password=<your password>" --project src/Rise.Server
   dotnet user-secrets list --project src/Rise.Server   # check that it is stored
   ```

   The secret is stored per machine outside the repository (`%APPDATA%\Microsoft\UserSecrets\` on Windows, `~/.microsoft/usersecrets/` on Linux/macOS). `appsettings.json` only contains an empty `DatabaseConnection` placeholder. In Development the app and `dotnet ef` read the secret automatically and it overrides the placeholder. Other environments must provide the environment variable `ConnectionStrings__DatabaseConnection`. If the connection string is missing, the app stops at startup with a message showing this command.

6. Run the project using `Rise.Server` as the startup project.

7. The project should open in your default browser on port `5001`. In Development the database is dropped, migrated and seeded on every startup.

8. Optional: start pgAdmin to browse the database:

   ```bash
   docker compose --profile tools up -d
   ```

   Open [http://localhost:5050](http://localhost:5050). The `Rise (dev)` server is already registered and connects without a password prompt.

   To stop everything use `docker compose --profile tools down`. Add `-v` to also remove the database volume.

## Docker Deployment

The `Dockerfile` builds a production image of `Rise.Server`, which also serves the Blazor client. It is a Release build that runs in the `Production` environment, listens on plain HTTP port 8080 and runs as a non-root user. There are three compose files:

| File | Purpose |
| --- | --- |
| `docker-compose.yml` | Development: only PostgreSQL (+ pgAdmin), the app runs with `dotnet run`. |
| `docker-compose.local.yml` | Local test deployment: app container + its own database (project `rise-local`). |
| `docker-compose.prod.yml` | Preparation for the real deployment (not used by `deploy.yaml` yet). |

### Local test deployment

```bash
docker compose -f docker-compose.local.yml up -d --build   # http://localhost:8080
docker compose -f docker-compose.local.yml logs -f app
docker compose -f docker-compose.local.yml down -v         # stop and reset the database
```

- Uses the same `.env` as development (`POSTGRES_PASSWORD` is required). It has its own database volume and no published DB port, so it runs side by side with the dev stack and dev runs never wipe its data.
- On startup the app applies the migrations and seeds the test data (`APP_SEED=true`, same users as in development). The seeder skips data that already exists, so restarts are safe.
- Stripe: the app needs `Stripe:SecretKey` at startup. Without `STRIPE_SECRET_KEY` in `.env` it gets a placeholder, so the app starts but payments fail. To test payments, put your Stripe test keys in `.env` (`STRIPE_SECRET_KEY`, `STRIPE_WEBHOOK_SECRET`, `STRIPE_SUCCESS_URL`, `STRIPE_CANCEL_URL`).

### Configuration

Outside Development, the database is only migrated or seeded when these settings are on (both default to `false` in `appsettings.json`):

| Environment variable | Meaning |
| --- | --- |
| `ConnectionStrings__DatabaseConnection` | PostgreSQL connection string (required) |
| `Database__MigrateOnStartup` | Apply EF migrations on startup (without deleting the database) |
| `Database__Seed` | Run `DbSeeder` after migrating |
| `Stripe__SecretKey`, `Stripe__WebhookSecret`, `Stripe__SuccessUrl`, `Stripe__CancelUrl`, `Stripe__Currency` | Stripe settings |
| `UseHttpsRedirection` | `false` in the image, TLS is terminated by a reverse proxy |

### Real deployment (preparation)

`docker-compose.prod.yml` runs the image `${RISE_IMAGE:-rise-server:latest}` with migrations on and seeding off. The app is only bound to `127.0.0.1:${APP_PORT:-8080}`, and all Stripe variables are required. Put a reverse proxy (nginx, Caddy, …) in front of it that terminates TLS and forwards `X-Forwarded-For` and `X-Forwarded-Proto`; the app evaluates these headers. To switch `.github/workflows/deploy.yaml` over later, the runner would:

```bash
docker build -t rise-server:latest .
docker compose -f docker-compose.prod.yml up -d
```

## Testing Stripe Payments Locally

Guests pay with [Stripe Checkout](https://docs.stripe.com/payments/checkout) in test mode, so no real money is involved. **Pay now** on the booking overview saves the booking and redirects to Stripe. After paying, Stripe sends the guest back to `/booking/confirmation`. If the guest cancels, Stripe sends them to `/payment/cancel`.

The payment is only marked as paid when Stripe calls our webhook (`POST /api/payments/stripe-webhook`). Stripe can't reach `localhost`, so the Stripe CLI has to forward the webhook. **If `stripe listen` isn't running while you pay, the booking stays "Payment processing" forever.**

### One-time setup

1. Install the Stripe CLI, then open a **new** PowerShell window (restart Visual Studio / Rider if it was open). You don't need `stripe login`, because the CLI uses the test secret key from User Secrets.

   ```powershell
   winget install Stripe.StripeCli
   stripe --version
   ```

2. Add the Stripe settings to User Secrets. Ask the team for the test secret key, and **never commit or share `sk_test_...` / `whsec_...` values**.

   ```powershell
   cd "<repo root>"
   dotnet user-secrets set "Stripe:SecretKey"  "sk_test_..." --project .\src\Rise.Server
   dotnet user-secrets set "Stripe:Currency"   "eur" --project .\src\Rise.Server
   dotnet user-secrets set "Stripe:SuccessUrl" "https://localhost:5001/booking/confirmation?session_id={CHECKOUT_SESSION_ID}" --project .\src\Rise.Server
   dotnet user-secrets set "Stripe:CancelUrl"  "https://localhost:5001/payment/cancel" --project .\src\Rise.Server
   ```

3. Store the webhook signing secret. `stripe listen` prints it, and it stays the same for your machine and Stripe account:

   ```powershell
   $key = (dotnet user-secrets list --project .\src\Rise.Server | Select-String '^Stripe:SecretKey = ').Line -replace '^Stripe:SecretKey = ', ''
   $whsec = stripe listen --api-key $key --print-secret
   dotnet user-secrets set "Stripe:WebhookSecret" $whsec --project .\src\Rise.Server
   ```

   To check which keys are set (names only, no values): `dotnet user-secrets list --project .\src\Rise.Server | % { ($_ -split ' = ')[0] }`. You need `Stripe:SecretKey`, `Stripe:Currency`, `Stripe:SuccessUrl`, `Stripe:CancelUrl` and `Stripe:WebhookSecret`.

### Every test run

Use PowerShell (not cmd) with two windows. **Start window 1 before you pay.**

**Window 1: Stripe listener** (keep it open)

```powershell
cd "<repo root>"
$key = (dotnet user-secrets list --project .\src\Rise.Server | Select-String '^Stripe:SecretKey = ').Line -replace '^Stripe:SecretKey = ', ''
stripe listen --api-key $key --events checkout.session.completed,checkout.session.async_payment_succeeded,checkout.session.async_payment_failed --forward-to https://localhost:5001/api/payments/stripe-webhook
```

**Window 2: database and server**

```powershell
cd "<repo root>"
docker compose up -d
dotnet run --project .\src\Rise.Server\Rise.Server.csproj
```

Wait for `Now listening on: https://localhost:5001`. The server reads User Secrets only at startup, so restart it after you change a secret.

**In the browser**

1. Open https://localhost:5001. Search for dates, pick a room, enter the guest details and click **Pay now**.
2. On the Stripe page, pay with card `4242 4242 4242 4242`, any future date, any CVC and any name. Other test cards (declined, 3D Secure, …) are listed in the [Stripe docs](https://docs.stripe.com/testing).
3. You land on `/booking/confirmation?session_id=cs_test_...`. The page shows "We're confirming your payment…" for a moment, then **Confirmed**.
4. Window 1 shows `--> checkout.session.completed` followed by `<-- [200] POST https://localhost:5001/api/payments/stripe-webhook`.
5. Optional: log in as `admin@example.com` / `A1b2C3!` and open `/invoice-list`. The booking shows **Paid**, via Stripe, with "Visa •••• 4242 · Stripe receipt ↗".

**Cancel flow:** click the back arrow on the Stripe page. You land on `/payment/cancel`, and the booking stays unpaid.

**Without the UI:** booking 2 (`20261003-0002`) is an open booking in the seed data. `curl.exe -k -X POST https://localhost:5001/api/bookings/2/checkout-session -H "Accept: application/json"` returns a `checkoutUrl` that you can open in the browser.

Press Ctrl+C in both windows to stop. `docker compose down` stops the database.

### Troubleshooting

| Problem | Fix |
| --- | --- |
| Confirmation page stays on "Payment processing" | `stripe listen` wasn't running when you paid, so the webhook never arrived. Start window 1 and pay again. Alternatively, run `stripe events resend <evt_id> --api-key $key` (the event id is in the Stripe dashboard or `stripe events list --api-key $key`). Resending only works while the server is still in the same run, because a restart reseeds the database. |
| Window 1 shows `[400]` for the webhook | `Stripe:WebhookSecret` doesn't match the secret `stripe listen` prints. Set it again (one-time setup, step 3) and restart the server. |
| Window 1 shows `[404]` for the webhook | The payment doesn't exist anymore. In Development, every server start deletes and reseeds the database, so bookings from an earlier run are gone. |
| `stripe` not found | Open a new terminal, or reload PATH: `$env:Path = [Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [Environment]::GetEnvironmentVariable("Path","User")` |
| `$key = ...` doesn't work | You're in cmd. Type `powershell` first. |
| "You have not configured API keys" | `--api-key $key` is missing or `$key` is empty. Run the `$key = ...` line again in the same window. |
| "Stripe:SecretKey is not configured" | That secret is missing from User Secrets. |
| Database or connection error at startup | Docker isn't running, or the password in the connection string doesn't match `.env`. |
| Migration error | Old database state. Run `docker compose down -v` (this deletes all local data), then `docker compose up -d`. |
| Paid booking shows no card details | Only payments made after the Stripe details were added store them. Make a new test payment. |

## Creation of the Database

The database is created by the application itself using Entity Framework Core migrations.

To add and remove migrations, install the `dotnet-ef` tool globally:

```bash
dotnet tool install --global dotnet-ef
```

You only need to do this once.

If you already have `dotnet-ef` installed, you can update it using:

```bash
dotnet tool update --global dotnet-ef
```

## Migrations

Database schema changes are managed using Entity Framework Core migrations.

To create a new migration, run the following command from the `src` folder:

```bash
dotnet ef migrations add YourMigrationName --startup-project Rise.Server --project Rise.Persistence
```

Then update the database using:

```bash
dotnet ef database update --startup-project Rise.Server --project Rise.Persistence
```

Alternatively, simply run `Rise.Server` if the application automatically applies migrations during startup.

## Useful Commands

Run these commands from the `src/Rise.Server` folder.

### Watch

```bash
dotnet watch --non-interactive
```

The `dotnet watch` command watches your source files for changes.

When a change is detected, it either applies Hot Reload or restarts the application when the change cannot be applied dynamically. This enables fast iterative development from the command line.

### Run

```bash
dotnet run
```

The `dotnet run` command builds and runs the application directly from the source code.

### Clean

```bash
dotnet clean
```

You generally won't need this command often.

It cleans the output of previous builds, including the intermediate `obj` and final `bin` directories.

## Authentication

Authentication and authorization are included.

User accounts are hosted and maintained in the application's own database without requiring an external identity provider.

You can log in as the seeded administrator with the password:

```text
A1b2C3!
```

### Users

- admin@example.com

### Roles

There is one built-in role:

- Administrator

### Use Cases

- Login
- Logout
- GetInfo

## Solution Structure Overview

The template is designed as a boilerplate for .NET 10 solutions and follows established practices for structuring projects, separation of concerns, and maintainability.

The architecture combines concepts from **Clean Architecture**, **Domain-Driven Design (DDD)** and **Vertical Slicing**.

The goal is to keep different aspects of the application separated and independent, making the application easier to maintain, test and extend.

The main projects are:

1. **Domain**
2. **Services**
3. **Persistence**
4. **Server**
5. **Client**
6. **Shared**
7. **Testing Projects**

---

### 1. Domain Project

**Folder:** `Domain`

**Purpose:** The **Domain** project contains the core business logic of the application.

It defines business rules independently from the UI, database and external technologies. The goal is to ensure that domain logic is not coupled to infrastructure or frameworks.

**Typical Contents:**

- **Entities**: Classes representing core concepts such as `Room`, `Guest` or `Booking`.
- **Value Objects**: Immutable objects representing concepts such as `Money` or `Address`.

> Value Objects are currently not provided in the template. You can read more in the [Domain-Driven Design course material](https://hogent-web.github.io/csharp/chapters/03/slides/index.html#75).

**Why this separation?**

Keeping domain logic separate ensures that business rules remain consistent even when the application's presentation, persistence or infrastructure changes.

---

### 2. Services Project

**Folder:** `Services`

**Purpose:** The **Services** project contains application-specific logic.

It orchestrates use cases, commands, queries and workflows and acts as an intermediary between the **Domain** and external layers.

**Typical Contents:**

- **Use Cases**: Classes responsible for specific operations, such as creating an order or processing a payment.

**Why this separation?**

This project enforces **Separation of Concerns (SoC)** and makes application logic easier to test independently from infrastructure.

The API could, for example, be replaced by a console application without changing the underlying business rules.

> We do not recommend abstracting Entity Framework Core behind a generic repository solely to make the database provider replaceable. Changing database providers should be treated as a migration rather than requiring another abstraction layer. See [Should you Abstract the Database?](https://enterprisecraftsmanship.com/posts/should-you-abstract-database/).

The project uses PostgreSQL (`Npgsql.EntityFrameworkCore.PostgreSQL`). For development it runs in Docker via `docker-compose.yml`.

---

### 3. Persistence Project

**Folder:** `Persistence`

**Purpose:** The **Persistence** project contains database mappings, migrations and persistence-related configuration.

**Typical Contents:**

- **Configurations**: Entity Framework Core entity configurations describing how domain entities are persisted.
- **Migrations**: Entity Framework Core migrations used to evolve the database schema.
- **Triggers**: Logic executed when entities are saved or retrieved. These triggers are database-provider agnostic and can therefore work with SQL Server, MariaDB, PostgreSQL and other supported providers.

**Why this separation?**

Persistence concerns are kept outside the **Domain** project.

Domain entities should not need to know how or where they are stored.

---

### 4. Server Project

**Folder:** `Server`

**Purpose:** The **Server** project is the entry point of the backend application.

It exposes HTTP endpoints, processes incoming requests and returns responses to clients.

The project uses **FastEndpoints** to expose application functionality.

**Typical Contents:**

- **Endpoints**: Handle HTTP requests and responses.
- **Processors**: Handle cross-cutting concerns such as logging and error handling.
- **Dependency Injection Configuration**: Registers services and application dependencies.
- **Blazor Client Hosting**: If no API endpoint matches the request, the server can serve the Blazor WebAssembly client. This simplifies deployment and avoids unnecessary CORS configuration.

### Centralized Response Handling

You might notice something interesting about how endpoints send responses.

In `Program.cs`, the FastEndpoints configuration contains:

```csharp
ep.DontAutoSendResponse();
```

This disables FastEndpoints' default automatic response handling.

Instead, the application uses a custom **Post-Processor** called `GlobalResponseSender`.

This processor runs after endpoints and is responsible for creating the final HTTP response. It takes the object returned by the endpoint—typically an `Ardalis.Result`—and maps it to the appropriate HTTP status code.

For example:

- `Result<RoomDto>` → `200 OK`
- `Result.Invalid(errors)` → `400 Bad Request`
- `Result.NotFound()` → `404 Not Found`

This keeps endpoint implementations focused on their use case while ensuring HTTP responses are handled consistently in one central location.

**Why this separation?**

The **Server** project provides a clear boundary between clients and the application's business logic.

External clients such as mobile applications, web applications or other services communicate with the application through a consistent HTTP API.

---

### 5. Client Project

**Folder:** `Client`

The **Client** project is a standalone Blazor WebAssembly application.

It serves the same architectural role as frontend applications written with frameworks such as React, Vue, Svelte or Angular, but is written in C# and runs on WebAssembly.

---

### 6. Shared Project

**Folder:** `Shared`

The **Shared** project contains the contracts shared between the **Client** and **Server**.

It decouples the Client from the Domain model. This allows the Domain, Services and Persistence layers to evolve without directly exposing domain entities to external clients.

As long as existing API contracts remain compatible, internal implementation details can change without breaking clients.

**Typical Contents:**

- **Service Interfaces**: Contracts between the **Client** and **Server/API**.
- **Data Transfer Objects (DTOs)**: Simple data structures used to transfer information between the API and its clients. DTOs should not contain domain logic.

---

### 7. Testing Projects

**Folders:**

- `Client.Tests`
- `Services.Tests`
- `Domain.Tests`

Testing is separated into dedicated projects to keep tests modular and focused on the components they verify.

Typical test categories include:

- **Unit Tests**: Test individual classes or components in isolation.
- **Component Tests**: Test Blazor components using bUnit.
- **Integration Tests**: Verify that multiple parts of the application work together correctly, such as the API and database.
- **End-to-End Tests**: Verify complete application workflows from a user's perspective.

> Integration tests are not provided by the template. You are expected to implement these yourself. The [FastEndpoints integration testing documentation](https://fast-endpoints.com/docs/integration-unit-testing#integration-testing) provides a useful starting point.

---

### 8. Cross-Cutting Concerns

Cross-cutting concerns are functionality that affects multiple parts of the application.

Examples include:

- Logging
- Caching
- Authentication
- Authorization
- Validation
- Exception handling

These concerns are typically implemented in the **Server** and **Persistence** projects or through reusable services.

---

## Key Concepts Explained

1. **Separation of Concerns (SoC)**  
   Each project has a well-defined responsibility. Changes to one part of the system should have minimal impact on unrelated parts.

2. **Dependency Injection (DI)**  
   Dependencies are provided to classes rather than created directly by them. The **Server** project acts as the composition root where most dependencies are registered.

3. **Domain-Driven Design (DDD)**  
   Business rules belong in the **Domain** and should remain independent from infrastructure and presentation concerns.

4. **Vertical Slicing**  
   Application functionality is organized around use cases or features rather than forcing all functionality into broad technical layers.

---

## Conclusion

The `Rise` solution is structured to encourage scalability, maintainability and testability.

Each project has a distinct responsibility:

- **Domain** defines the core business rules.
- **Services** implements and orchestrates application use cases.
- **Persistence** handles Entity Framework Core configuration and database persistence.
- **Server** exposes the application through HTTP and hosts the backend.
- **Client** provides the Blazor WebAssembly user interface.
- **Shared** defines contracts between the Client and Server.
- **Tests** verify the behavior of the individual layers and the application as a whole.

## Course

There is a .NET course from previous academic years. Although it is no longer actively maintained, much of the material is still relevant:

https://hogent-web.github.io/csharp/