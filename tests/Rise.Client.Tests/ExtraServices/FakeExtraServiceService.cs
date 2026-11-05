using Ardalis.Result;
using Rise.Shared.ExtraServices;

namespace Rise.Client.Tests.ExtraServices;

public class FakeExtraServiceService : IExtraServiceService
{
    public List<ExtraServiceDto.Index> Services { get; set; } =
    [
        new()
        {
            Id = 1,
            Name = "Breakfast",
            Description = "Morning buffet",
            Price = 15m,
            Cadence = ChargeCadence.PerNight,
            IsPerPerson = true,
            IsActive = true
        },
        new()
        {
            Id = 2,
            Name = "Extra Cleaning",
            Description = "Deep cleaning",
            Price = 45m,
            Cadence = ChargeCadence.Once,
            IsPerPerson = false,
            IsActive = false
        }
    ];

    public Task<Result<ExtraServiceResponse.Index>> GetIndexAsync(bool onlyActive = false, CancellationToken ctx = default)
    {
        var list = onlyActive ? Services.Where(s => s.IsActive) : Services;
        return Task.FromResult(Result.Success(new ExtraServiceResponse.Index { Services = list.ToList() }));
    }

    public Task<Result<ExtraServiceDto.Index>> GetByIdAsync(int id, CancellationToken ctx = default)
    {
        var item = Services.FirstOrDefault(s => s.Id == id);
        return Task.FromResult(item != null
            ? Result.Success(item)
            : Result<ExtraServiceDto.Index>.NotFound());
    }

    public Task<Result<ExtraServiceResponse.Create>> CreateAsync(ExtraServiceRequest.Create request, CancellationToken ctx = default)
    {
        var id = Services.Count + 1;
        var created = new ExtraServiceDto.Index
        {
            Id = id,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Cadence = request.Cadence,
            IsPerPerson = request.IsPerPerson,
            IsActive = true
        };
        Services.Add(created);

        return Task.FromResult(Result.Success(new ExtraServiceResponse.Create { Id = id, Name = request.Name }));
    }

    public Task<Result> EditAsync(ExtraServiceRequest.Edit request, CancellationToken ctx = default)
    {
        var item = Services.FirstOrDefault(s => s.Id == request.Id);
        if (item is null)
            return Task.FromResult(Result.NotFound());

        item.Name = request.Name;
        item.Description = request.Description;
        item.Price = request.Price;
        item.Cadence = request.Cadence;
        item.IsPerPerson = request.IsPerPerson;
        item.IsActive = request.IsActive;

        return Task.FromResult(Result.Success());
    }

    public Task<Result> ToggleActiveAsync(int id, CancellationToken ctx = default)
    {
        var item = Services.FirstOrDefault(s => s.Id == id);
        if (item is null)
            return Task.FromResult(Result.NotFound());

        item.IsActive = !item.IsActive;
        return Task.FromResult(Result.Success());
    }
}
