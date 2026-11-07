namespace Rise.Shared.Amenities;

/// <summary>
/// contract for the admin amenities page, the server implements it with the database, the client with http
/// </summary>
public interface IAmenityService
{
    /// <summary>
    /// all amenities that are not deleted, also the inactive ones
    /// </summary>
    Task<Result<AmenityResponse.Index>> GetIndexAsync(CancellationToken ctx = default);

    /// <summary>
    /// adds an active amenity, conflict when a name is already used in the same language
    /// </summary>
    Task<Result<AmenityResponse.Create>> CreateAsync(AmenityRequest.Create request, CancellationToken ctx = default);

    /// <summary>
    /// changes the names and icon, visible on every room type that offers it
    /// </summary>
    Task<Result> EditAsync(AmenityRequest.Edit request, CancellationToken ctx = default);

    /// <summary>
    /// shows the amenity to the guests again
    /// </summary>
    Task<Result> ActivateAsync(int amenityId, CancellationToken ctx = default);

    /// <summary>
    /// hides the amenity from the guests, it stays assigned to its room types
    /// </summary>
    Task<Result> DeactivateAsync(int amenityId, CancellationToken ctx = default);
}
