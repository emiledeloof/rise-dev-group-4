using Microsoft.EntityFrameworkCore;
using Rise.Persistence;
using Rise.Shared.Common;
using Rise.Shared.Rates;
using Rise.Shared.Rooms;

namespace Rise.Services.Rates;

public sealed class RateService(ApplicationDbContext dbContext) : IRateService
{
    public async Task<Result<RateResponse.Create>> CreateAsync(
        RateRequest.Create request,
        CancellationToken cancellationToken = default)
    {
        var room = await dbContext.Rooms
            .FirstOrDefaultAsync(r => r.Name == request.RoomName && !r.IsDeleted, cancellationToken);

        if (room is null)
        {
            return Result.NotFound("Room not found.");
        }

        var rate = room.AddRate(
            request.Name,
            request.PricePerNight,
            request.Description);

        dbContext.Rates.Add(rate);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new RateResponse.Create
        {
            Id = rate.Id,
            Name = rate.Name,
            RoomName = rate.Room.Name,
            PricePerNight = rate.PricePerNight,
            Description = rate.Description,
        });
    }

    public async Task<Result<RateResponse.Index>> GetIndexAsync(
        RateRequest.Index request,
        CancellationToken ctx = default)
    {

        var query = dbContext.Rates
            .Where(rate => !rate.IsDeleted && !rate.Room.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim();
            query = query.Where(rate =>
                rate.Name.Contains(searchTerm) ||
                rate.Room.Name.Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(request.RoomName))
            query = query.Where(rate => rate.Room.Name == request.RoomName);

        if (!string.IsNullOrWhiteSpace(request.Policy))
            query = query.Where(rate => rate.Description == request.Policy);

        var totalCount = await query.CountAsync(ctx);

        query = request.OrderBy switch
        {
            "Name" => request.OrderDescending
                ? query.OrderByDescending(rate => rate.Name)
                : query.OrderBy(rate => rate.Name),
            "RoomName" => request.OrderDescending
                ? query.OrderByDescending(rate => rate.Room.Name)
                : query.OrderBy(rate => rate.Room.Name),
            "PricePerNight" => request.OrderDescending
                ? query.OrderByDescending(rate => rate.PricePerNight)
                : query.OrderBy(rate => rate.PricePerNight),
            _ => query.OrderByDescending(rate => rate.Id),
        };

        var activeRates = dbContext.Rates
            .Where(rate => !rate.IsDeleted && !rate.Room.IsDeleted);

        var roomNames = await activeRates
            .Select(rate => rate.Room.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(ctx);

        var policies = await activeRates
            .Where(rate => rate.Description != null)
            .Select(rate => rate.Description!)
            .Distinct()
            .OrderBy(policy => policy)
            .ToListAsync(ctx);

        var rates = await query
            .AsNoTracking()
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(rate => new RateDto.Index
            {
                Id = rate.Id,
                Name = rate.Name,
                RoomName = rate.Room.Name,
                PricePerNight = rate.PricePerNight,
                Description = rate.Description,
            })
            .ToListAsync(ctx);

        return Result.Success(new RateResponse.Index
        {
            Rates = rates,
            RoomNames = roomNames,
            Policies = policies,
            TotalCount = totalCount,
        });
    }
}