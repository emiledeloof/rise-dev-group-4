namespace Rise.Shared.Guests;

/// <summary>
/// Defines the service contract for guest-related operations.
/// This contract is shared across both tiers:
/// 1. Server-side: Implemented by Rise.Services.Guests.GuestService (interacts with ApplicationDbContext).
/// 2. Client-side: Implemented by Rise.Client.Guests.GuestService (interacts with the API via HttpClient).
/// </summary>
public interface IGuestService
{
    /// <summary>
    /// Creates a new guest in the system.
    /// </summary>
    /// <param name="request">The guest input data validated by <see cref="GuestRequest.Create.Validator"/>.</param>
    /// <param name="ctx">Cancellation token for aborting the asynchronous operation if needed.</param>
    /// <returns>
    /// An Ardalis.Result indicating the outcome:
    /// - Success: Result.Created with the new GuestId (mapped to HTTP 201).
    /// - Duplicate: Result.Conflict if a guest with the email already exists (mapped to HTTP 409).
    /// - Validation failure: Result.Invalid (mapped to HTTP 400).
    /// </returns>
    Task<Result<GuestResponse.Create>> CreateAsync(GuestRequest.Create request, CancellationToken ctx = default);
}
