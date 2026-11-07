using Microsoft.EntityFrameworkCore;
using Rise.Domain.Rooms;
using Rise.Persistence;
using Rise.Shared.Amenities;
using Rise.Shared.Common;

namespace Rise.Services.Amenities;

/// <summary>
/// create, read, update and (de)activate of the amenities for the admin page
/// </summary>
public class AmenityService(ApplicationDbContext dbContext) : IAmenityService
{
    /// <summary>
    /// all amenities that are not deleted, active and inactive, sorted by English name, with the room types that offer them
    /// </summary>
    public async Task<Result<AmenityResponse.Index>> GetIndexAsync(CancellationToken ctx = default)
    {
        var rows = await dbContext.Amenities
            .AsNoTracking()
            .Where(a => !a.IsDeleted)
            .Select(a => new
            {
                a.Id,
                a.Icon,
                a.IsActive,
                Names = a.Translations.Select(t => new { t.Language, t.Name }).ToList(),
                RoomTypeNames = dbContext.RoomTypes
                    .Where(t => !t.IsDeleted && t.Amenities.Any(x => x.Id == a.Id))
                    .OrderBy(t => t.Name)
                    .Select(t => t.Name)
                    .ToList(),
            })
            .ToListAsync(ctx);

        var amenities = rows
            .Select(a => new AmenityDto.Admin
            {
                Id = a.Id,
                Icon = a.Icon,
                IsActive = a.IsActive,
                Names = a.Names.ToDictionary(n => n.Language, n => n.Name),
                RoomTypeNames = a.RoomTypeNames,
            })
            .OrderBy(a => a.Names.GetValueOrDefault(Languages.Default), StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Result.Success(new AmenityResponse.Index { Amenities = amenities });
    }

    /// <summary>
    /// adds an active amenity, conflict when an other amenity already has one of the names in the same language
    /// </summary>
    public async Task<Result<AmenityResponse.Create>> CreateAsync(AmenityRequest.Create request, CancellationToken ctx = default)
    {
        var conflict = await FindNameConflictAsync(request.Names, null, ctx);
        if (conflict is not null)
            return Result.Conflict(conflict);

        var amenity = new Amenity(request.Names[Languages.Default]!, request.Icon);
        ApplyNames(amenity, request.Names);
        dbContext.Amenities.Add(amenity);
        await dbContext.SaveChangesAsync(ctx);

        Log.Information("Amenity {AmenityId} created", amenity.Id);

        return Result.Success(new AmenityResponse.Create
        {
            Id = amenity.Id,
            Name = amenity.DefaultName,
        });
    }

    /// <summary>
    /// changes the names and icon, every room type that offers it shows the change
    /// </summary>
    public async Task<Result> EditAsync(AmenityRequest.Edit request, CancellationToken ctx = default)
    {
        // ! tracked on purpose, entity framework must follow the amenity and its names to save the changes
        var amenity = await dbContext.Amenities
            .FirstOrDefaultAsync(a => a.Id == request.AmenityId && !a.IsDeleted, ctx);

        if (amenity is null)
            return Result.NotFound($"Amenity with id '{request.AmenityId}' was not found.");

        // the amenity itself keeps its names, only the other amenities count
        var conflict = await FindNameConflictAsync(request.Names, amenity.Id, ctx);
        if (conflict is not null)
            return Result.Conflict(conflict);

        ApplyNames(amenity, request.Names);
        amenity.Icon = request.Icon;
        await dbContext.SaveChangesAsync(ctx);

        Log.Information("Amenity {AmenityId} updated", amenity.Id);

        return Result.Success();
    }

    /// <summary>
    /// shows the amenity to the guests again, on every room type it is still assigned to
    /// </summary>
    public Task<Result> ActivateAsync(int amenityId, CancellationToken ctx = default)
        => ChangeActiveAsync(amenityId, true, ctx);

    /// <summary>
    /// hides the amenity from the guests, it stays in the database and assigned to its room types
    /// </summary>
    public Task<Result> DeactivateAsync(int amenityId, CancellationToken ctx = default)
        => ChangeActiveAsync(amenityId, false, ctx);

    private async Task<Result> ChangeActiveAsync(int amenityId, bool isActive, CancellationToken ctx)
    {
        var amenity = await dbContext.Amenities
            .FirstOrDefaultAsync(a => a.Id == amenityId && !a.IsDeleted, ctx);

        if (amenity is null)
            return Result.NotFound($"Amenity with id '{amenityId}' was not found.");

        if (isActive)
            amenity.Activate();
        else
            amenity.Deactivate();
        await dbContext.SaveChangesAsync(ctx);

        Log.Information("Amenity {AmenityId} {State}", amenity.Id, isActive ? "activated" : "deactivated");

        return Result.Success();
    }

    /// <summary>
    /// sets every supported language, a language missing in the request has no translation afterwards
    /// </summary>
    private static void ApplyNames(Amenity amenity, IReadOnlyDictionary<string, string?> names)
    {
        foreach (var language in Languages.Supported)
            amenity.SetName(language, names.GetValueOrDefault(language));
    }

    /// <summary>
    /// the message for the first name an other amenity already uses in the same language, null when all names are free
    /// </summary>
    private async Task<string?> FindNameConflictAsync(IReadOnlyDictionary<string, string?> names, int? ignoredId, CancellationToken ctx)
    {
        foreach (var language in Languages.Supported)
        {
            var name = names.GetValueOrDefault(language)?.Trim();
            if (string.IsNullOrEmpty(name))
                continue;

            // ? postgres compares strings with case, so both sides go to lower case
            var lowerName = name.ToLower();
            var isTaken = await dbContext.Amenities.AnyAsync(a =>
                !a.IsDeleted
                && a.Id != ignoredId
                && a.Translations.Any(t => t.Language == language && t.Name.ToLower() == lowerName), ctx);

            if (isTaken)
            {
                return language == Languages.Default
                    ? $"An amenity named '{name}' already exists."
                    : $"An amenity named '{name}' already exists in {Languages.Names[language]}.";
            }
        }

        return null;
    }
}
