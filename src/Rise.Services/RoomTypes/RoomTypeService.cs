using Microsoft.EntityFrameworkCore;
using Rise.Domain.Rooms;
using Rise.Persistence;
using Rise.Shared.Common;
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
    /// one room type with its amenities and bed configurations, not found when it does not exist or is deleted
    /// </summary>
    public async Task<Result<RoomTypeDto.Edit>> GetForEditAsync(int roomTypeId, CancellationToken ctx = default)
    {
        var row = await dbContext.RoomTypes
            .AsNoTracking()
            .Where(t => t.Id == roomTypeId && !t.IsDeleted)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Description,
                AmenityIds = t.Amenities.Where(a => !a.IsDeleted).Select(a => a.Id).ToList(),
                Beds = t.Translations.Select(x => new { x.Language, x.BedConfiguration }).ToList(),
            })
            .SingleOrDefaultAsync(ctx);

        if (row is null)
            return Result.NotFound($"Room type with id '{roomTypeId}' was not found.");

        return Result.Success(new RoomTypeDto.Edit
        {
            Id = row.Id,
            Name = row.Name,
            Description = row.Description,
            AmenityIds = row.AmenityIds,
            BedConfigurations = row.Beds.ToDictionary(b => b.Language, b => b.BedConfiguration),
        });
    }

    /// <summary>
    /// adds a room type, conflict when an other type already has the name
    /// </summary>
    public async Task<Result<RoomTypeResponse.Create>> CreateAsync(RoomTypeRequest.Create request, CancellationToken ctx = default)
    {
        if (await NameIsTakenAsync(request.Name, null, ctx))
            return Result.Conflict($"A room type named '{request.Name.Trim()}' already exists.");

        var roomType = new RoomType(request.Name, request.Description);
        var characteristicsError = await ApplyCharacteristicsAsync(roomType, request, ctx);
        if (characteristicsError is not null)
            return Result.Invalid(new ValidationError(characteristicsError));

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
    /// changes the name, description, amenities and beds, not found when the type does not exist or is deleted
    /// </summary>
    public async Task<Result> EditAsync(RoomTypeRequest.Edit request, CancellationToken ctx = default)
    {
        // ! tracked on purpose, entity framework must follow the type and its amenities to save the changes
        var roomType = await dbContext.RoomTypes
            .Include(t => t.Amenities)
            .FirstOrDefaultAsync(t => t.Id == request.RoomTypeId && !t.IsDeleted, ctx);

        if (roomType is null)
            return Result.NotFound($"Room type with id '{request.RoomTypeId}' was not found.");

        // the type itself keeps its name, only the other types count
        if (await NameIsTakenAsync(request.Name, roomType.Id, ctx))
            return Result.Conflict($"A room type named '{request.Name.Trim()}' already exists.");

        var characteristicsError = await ApplyCharacteristicsAsync(roomType, request, ctx);
        if (characteristicsError is not null)
            return Result.Invalid(new ValidationError(characteristicsError));

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
    /// sets the ticked amenities and the beds of every language on the type
    /// the form only shows the active amenities, so the inactive ones already assigned are kept, reactivating them shows them again
    /// returns the error when an id is unknown, deleted, or an inactive amenity the type did not have yet, null when all went well
    /// </summary>
    private async Task<string?> ApplyCharacteristicsAsync(RoomType roomType, RoomTypeRequest.Form request, CancellationToken ctx)
    {
        var requestedIds = request.AmenityIds.Distinct().ToList();
        var assigned = roomType.Amenities.Where(a => !a.IsDeleted).ToList();
        var assignedIds = assigned.Select(a => a.Id).ToList();

        var requested = await dbContext.Amenities
            .Where(a => requestedIds.Contains(a.Id) && !a.IsDeleted && (a.IsActive || assignedIds.Contains(a.Id)))
            .ToListAsync(ctx);

        if (requested.Count != requestedIds.Count)
            return "One of the amenities does not exist or is inactive.";

        var keptInactive = assigned.Where(a => !a.IsActive);
        roomType.ReplaceAmenities(requested.Concat(keptInactive));

        foreach (var language in Languages.Supported)
            roomType.SetBedConfiguration(language, request.BedConfigurations.GetValueOrDefault(language));

        return null;
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
