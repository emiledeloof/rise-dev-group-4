namespace Rise.Shared.RoomTypes;

/// <summary>
/// contract for the admin room types page, the server implements it with the database, the client with http
/// </summary>
public interface IRoomTypeService
{
    /// <summary>
    /// all room types that are not deleted
    /// </summary>
    Task<Result<RoomTypeResponse.Index>> GetIndexAsync(CancellationToken ctx = default);

    /// <summary>
    /// adds a room type, conflict when the name is already used
    /// </summary>
    Task<Result<RoomTypeResponse.Create>> CreateAsync(RoomTypeRequest.Create request, CancellationToken ctx = default);

    /// <summary>
    /// changes the name and description of a room type
    /// </summary>
    Task<Result> EditAsync(RoomTypeRequest.Edit request, CancellationToken ctx = default);

    /// <summary>
    /// deletes a room type, conflict when a room still uses it
    /// </summary>
    Task<Result> DeleteAsync(int roomTypeId, CancellationToken ctx = default);
}
