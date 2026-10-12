using Microsoft.EntityFrameworkCore;
using Rise.Domain.Guests;
using Rise.Persistence;
using Rise.Shared.Guests;
using Serilog;

namespace Rise.Services.Guests;

/// <summary>
/// Server-side implementation of <see cref="IGuestService"/>.
/// Handles business logic, domain entity instantiation, and database persistence.
/// Directly accesses <see cref="ApplicationDbContext"/> following project architecture (no repository abstraction).
/// </summary>
/// <param name="dbContext">The EF Core database context injected via dependency injection.</param>
public class GuestService(ApplicationDbContext dbContext) : IGuestService
{
    /// <summary>
    /// Creates a new guest in the database after verifying business constraints.
    /// </summary>
    /// <param name="request">Validated input data containing guest details and address.</param>
    /// <param name="ctx">Cancellation token for aborting the operation.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing <see cref="GuestResponse.Create"/> with the new Guest ID,
    /// or <see cref="ResultStatus.Conflict"/> if the email is already in use.
    /// </returns>
    public async Task<Result<GuestResponse.Create>> CreateAsync(GuestRequest.Create request, CancellationToken ctx = default)
    {
        // 1. Business constraint check: Check for existing guest with the same email.
        // A unique index is defined in GuestConfiguration; checking beforehand prevents raw database exceptions.
        var emailNormalized = request.Email.Trim();
        if (await dbContext.Guests.AnyAsync(g => g.Email == emailNormalized, ctx))
        {
            Log.Warning("Guest with email '{Email}' already exists.", emailNormalized);
            return Result.Conflict($"A guest with email '{emailNormalized}' already exists.");
        }

        // 2. Instantiate Value Object: GuestAddress encapsulates and validates address fields via Guard.Against.
        var address = new GuestAddress(
            request.AddressLine1,
            request.AddressLine2,
            request.Country
        );

        // 3. Compose full name from first and last name.
        var fullName = $"{request.FirstName.Trim()} {request.LastName.Trim()}";

        // 4. Instantiate Domain Entity: Guest enforces invariants (non-empty fields, valid address).
        var guest = new Guest(
            fullName,
            emailNormalized,
            request.PhoneNumber.Trim(),
            address
        );

        // 5. Track entity in ApplicationDbContext.
        dbContext.Guests.Add(guest);

        // 6. Persist changes to PostgreSQL.
        // EntityBeforeSaveTrigger automatically handles CreatedAt and UpdatedAt UTC timestamps.
        await dbContext.SaveChangesAsync(ctx);

        Log.Information("Guest '{Name}' (ID: {GuestId}) created successfully.", guest.Name, guest.Id);

        // 7. Return Result.Created with the generated database ID.
        // Processors/GlobalResponseSender will map this to HTTP 201 Created on the server.
        return Result.Created(new GuestResponse.Create
        {
            GuestId = guest.Id
        });
    }
}
