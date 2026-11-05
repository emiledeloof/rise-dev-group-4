using Microsoft.EntityFrameworkCore;
using Rise.Persistence;
using Rise.Shared.ExtraServices;
using DomainCadence = Rise.Domain.ExtraServices.ChargeCadence;
using DomainExtraService = Rise.Domain.ExtraServices.ExtraService;
using SharedCadence = Rise.Shared.ExtraServices.ChargeCadence;

namespace Rise.Services.ExtraServices;

/// <summary>
/// Handles business logic and persistence for extra services.
/// </summary>
public class ExtraServiceService(ApplicationDbContext dbContext) : IExtraServiceService
{
    public async Task<Result<ExtraServiceResponse.Index>> GetIndexAsync(bool onlyActive = false, CancellationToken ctx = default)
    {
        var query = dbContext.ExtraServices
            .AsNoTracking()
            .Where(s => !s.IsDeleted);

        if (onlyActive)
        {
            query = query.Where(s => s.IsActive);
        }

        var services = await query
            .OrderBy(s => s.Name)
            .Select(s => new ExtraServiceDto.Index
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Price = s.Price,
                Cadence = (SharedCadence)s.Cadence,
                IsPerPerson = s.IsPerPerson,
                IsActive = s.IsActive
            })
            .ToListAsync(ctx);

        return Result.Success(new ExtraServiceResponse.Index { Services = services });
    }

    public async Task<Result<ExtraServiceDto.Index>> GetByIdAsync(int id, CancellationToken ctx = default)
    {
        var service = await dbContext.ExtraServices
            .AsNoTracking()
            .Where(s => s.Id == id && !s.IsDeleted)
            .Select(s => new ExtraServiceDto.Index
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Price = s.Price,
                Cadence = (SharedCadence)s.Cadence,
                IsPerPerson = s.IsPerPerson,
                IsActive = s.IsActive
            })
            .FirstOrDefaultAsync(ctx);

        if (service is null)
            return Result.NotFound($"Extra service with ID '{id}' was not found.");

        return Result.Success(service);
    }

    public async Task<Result<ExtraServiceResponse.Create>> CreateAsync(ExtraServiceRequest.Create request, CancellationToken ctx = default)
    {
        if (await NameIsTakenAsync(request.Name, null, ctx))
            return Result.Conflict($"An extra service named '{request.Name.Trim()}' already exists.");

        var service = new DomainExtraService(
            request.Name,
            request.Price,
            (DomainCadence)request.Cadence,
            request.IsPerPerson,
            request.Description);

        dbContext.ExtraServices.Add(service);
        await dbContext.SaveChangesAsync(ctx);

        Log.Information("Extra service {ExtraServiceId} '{Name}' created", service.Id, service.Name);

        return Result.Success(new ExtraServiceResponse.Create
        {
            Id = service.Id,
            Name = service.Name
        });
    }

    public async Task<Result> EditAsync(ExtraServiceRequest.Edit request, CancellationToken ctx = default)
    {
        var service = await dbContext.ExtraServices
            .FirstOrDefaultAsync(s => s.Id == request.Id && !s.IsDeleted, ctx);

        if (service is null)
            return Result.NotFound($"Extra service with ID '{request.Id}' was not found.");

        if (await NameIsTakenAsync(request.Name, service.Id, ctx))
            return Result.Conflict($"An extra service named '{request.Name.Trim()}' already exists.");

        service.Update(
            request.Name,
            request.Price,
            (DomainCadence)request.Cadence,
            request.IsPerPerson,
            request.Description);

        service.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync(ctx);

        Log.Information("Extra service {ExtraServiceId} updated", service.Id);

        return Result.Success();
    }

    public async Task<Result> ToggleActiveAsync(int id, CancellationToken ctx = default)
    {
        var service = await dbContext.ExtraServices
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, ctx);

        if (service is null)
            return Result.NotFound($"Extra service with ID '{id}' was not found.");

        if (service.IsActive)
            service.Deactivate();
        else
            service.Activate();

        await dbContext.SaveChangesAsync(ctx);

        Log.Information("Extra service {ExtraServiceId} active status set to {IsActive}", service.Id, service.IsActive);

        return Result.Success();
    }

    private async Task<bool> NameIsTakenAsync(string name, int? currentId, CancellationToken ctx)
    {
        var trimmedName = name.Trim().ToLower();

        return await dbContext.ExtraServices
            .AnyAsync(s => !s.IsDeleted
                && (currentId == null || s.Id != currentId)
                && s.Name.ToLower() == trimmedName, ctx);
    }
}
