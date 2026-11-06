namespace Rise.Shared.Rooms;

/// <summary>contract for the admin manage rooms page, kept apart from IRoomService so the guest side does not change</summary>
public interface IRoomAdminService
{
    /// <summary>all rooms that are not deleted, sorted by room number</summary>
    Task<Result<IReadOnlyList<RoomAdminDto.Index>>> GetIndexAsync(CancellationToken ctx = default);

    /// <summary>adds a physical room and gives its id, conflict when the room number is already used</summary>
    Task<Result<int>> CreateAsync(RoomRequest.Create request, CancellationToken ctx = default);
}
