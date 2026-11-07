using Microsoft.EntityFrameworkCore;
using Rise.Domain.Rooms;
using Rise.Persistence;
using Rise.Shared.RoomTypes;

namespace Rise.Services.RoomTypes;

/// <summary>
/// create, read, update and delete of the room types for the admin page
/// </summary>
public class RoomTypeService(ApplicationDbContext dbContext) : IRoomTypeService
{
    /// <summary>
    /// all room types that are not deleted, sorted by name, with their number of rooms
    /// </summary>
    public async Task<Result<RoomTypeResponse.Index>> GetIndexAsync(CancellationToken ctx = default)
    {
        var roomTypes = await dbContext.RoomTypes
            .AsNoTracking()
            .Where(t => !t.IsDeleted)
            .OrderBy(t => t.Name)
            .Select(t => new RoomTypeDto.Index
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                // ! compared on the id, the == of Entity is a custom operator that sql does not know
                // a room without type gives null in sql so it is never listed
                RoomNumbers = dbContext.Rooms
                    .Where(r => !r.IsDeleted && r.RoomType.Id == t.Id)
                    .OrderBy(r => r.RoomNumber)
                    .Select(r => r.RoomNumber)
                    .ToList(),
            })
            .ToListAsync(ctx);

        return Result.Success(new RoomTypeResponse.Index { RoomTypes = roomTypes });
    }

    /// <summary>
    /// adds a room type, conflict when an other type already has the name
    /// </summary>
    public async Task<Result<RoomTypeResponse.Create>> CreateAsync(RoomTypeRequest.Create request, CancellationToken ctx = default)
    {
        if (await NameIsTakenAsync(request.Name, null, ctx))
            return Result.Conflict($"A room type named '{request.Name.Trim()}' already exists.");

        var roomType = new RoomType(request.Name, request.Description);
        dbContext.RoomTypes.Add(roomType);
        await dbContext.SaveChangesAsync(ctx);

        Log.Information("Room type {RoomTypeId} created", roomType.Id);

        return Result.Success(new RoomTypeResponse.Create
        {
            Id = roomType.Id,
            Name = roomType.Name,
        });
    }

    /// <summary>
    /// changes the name and description, not found when the type does not exist or is deleted
    /// </summary>
    public async Task<Result> EditAsync(RoomTypeRequest.Edit request, CancellationToken ctx = default)
    {
        // ! tracked on purpose, entity framework must follow the type to save the changes
        var roomType = await dbContext.RoomTypes
            .FirstOrDefaultAsync(t => t.Id == request.RoomTypeId && !t.IsDeleted, ctx);

        if (roomType is null)
            return Result.NotFound($"Room type with id '{request.RoomTypeId}' was not found.");

        // the type itself keeps its name, only the other types count
        if (await NameIsTakenAsync(request.Name, roomType.Id, ctx))
            return Result.Conflict($"A room type named '{request.Name.Trim()}' already exists.");

        roomType.Name = request.Name;
        roomType.Description = request.Description;
        await dbContext.SaveChangesAsync(ctx);

        Log.Information("Room type {RoomTypeId} updated", roomType.Id);

        return Result.Success();
    }

    /// <summary>
    /// soft deletes a room type, conflict when a room still uses it
    /// </summary>
    public async Task<Result> DeleteAsync(int roomTypeId, CancellationToken ctx = default)
    {
        var roomType = await dbContext.RoomTypes
            .FirstOrDefaultAsync(t => t.Id == roomTypeId && !t.IsDeleted, ctx);

        if (roomType is null)
            return Result.NotFound($"Room type with id '{roomTypeId}' was not found.");

        // ! a room without its type would break the room pages later
        var isUsed = await dbContext.Rooms.AnyAsync(r => !r.IsDeleted && r.RoomType.Id == roomTypeId, ctx);
        if (isUsed)
            return Result.Conflict($"'{roomType.Name}' is still used by a room and cannot be deleted.");

        // * the before save trigger turns this remove into a soft delete
        dbContext.RoomTypes.Remove(roomType);
        await dbContext.SaveChangesAsync(ctx);

        Log.Information("Room type {RoomTypeId} deleted", roomTypeId);

        return Result.Success();
    }

    /// <summary>
    /// true when an other room type that is not deleted already has this name, case is ignored
    /// </summary>
    private Task<bool> NameIsTakenAsync(string name, int? ignoredId, CancellationToken ctx)
    {
        // ? postgres compares strings with case, so both sides go to lower case
        var lowerName = name.Trim().ToLower();
        return dbContext.RoomTypes.AnyAsync(t =>
            !t.IsDeleted
            && t.Id != ignoredId
            && t.Name.ToLower() == lowerName, ctx);
    }
}
