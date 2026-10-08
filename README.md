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

Then run `Rise.Server` (https://localhost:5001). See [Installation Instructions](#installation-instructions) for details.

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

You can log in using the following test users with the password:

```text
A1b2C3!
```

### Users

- user@example.com
- technician1@example.com
- technician2@example.com
- secretary@example.com
- admin@example.com

### Roles

There are three built-in roles, but these can be adjusted as needed:

- Technician
- Secretary
- Administrator

### Use Cases

- Register
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

- **Entities**: Classes representing core concepts such as `Order`, `Customer` or `Product`.
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

- `Result<ProductDto>` → `200 OK`
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