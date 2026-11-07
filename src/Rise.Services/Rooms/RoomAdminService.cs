using Microsoft.EntityFrameworkCore;
using Rise.Domain.Rooms;
using Rise.Persistence;
using Rise.Shared.Rooms;

namespace Rise.Services.Rooms;

/// <summary>physical rooms for the admin manage rooms page, apart from RoomService which serves the guest side</summary>
public class RoomAdminService(ApplicationDbContext dbContext) : IRoomAdminService
{
    /// <summary>all rooms that are not deleted, sorted by room number, with the name of their type</summary>
    public async Task<Result<IReadOnlyList<RoomAdminDto.Index>>> GetIndexAsync(CancellationToken ctx = default)
    {
        // * typed as IReadOnlyList so the result matches the return type, var would give a List
        IReadOnlyList<RoomAdminDto.Index> rooms = await dbContext.Rooms
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .OrderBy(r => r.RoomNumber)
            .Select(r => new RoomAdminDto.Index
            {
                Id = r.Id,
                RoomNumber = r.RoomNumber,
                MaxGuests = r.MaxGuests,
                RoomTypeName = r.RoomType.Name,
            })
            .ToListAsync(ctx);

        return Result.Success(rooms);
    }

    /// <summary>adds a physical room, conflict when the number is taken, not found when the type does not exist</summary>
    public async Task<Result<int>> CreateAsync(RoomRequest.Create request, CancellationToken ctx = default)
    {
        var roomType = await dbContext.RoomTypes
            .FirstOrDefaultAsync(t => t.Id == request.RoomTypeId && !t.IsDeleted, ctx);

        if (roomType is null)
            return Result.NotFound("Room type not found.");

        // the number without spaces around, so "101 " and "101" are the same room
        var number = request.RoomNumber.Trim();

        // ! a deleted room frees its number
        var isTaken = await dbContext.Rooms
            .AnyAsync(r => !r.IsDeleted && r.RoomNumber == number, ctx);

        if (isTaken)
            return Result.Conflict($"Room {number} already exists.");

        // ! the number is in the name so two rooms of a type never share a name, the rates still find a room by its name
        // TODO: drop the name once the rates are linked to the room type
        var room = new Room(number, $"{roomType.Name} {number}", request.MaxGuests, roomType);
        dbContext.Rooms.Add(room);
        await dbContext.SaveChangesAsync(ctx);

        Log.Information("Room {RoomNumber} created with type {RoomTypeId}", number, roomType.Id);

        return Result.Success(room.Id);
    }
}
