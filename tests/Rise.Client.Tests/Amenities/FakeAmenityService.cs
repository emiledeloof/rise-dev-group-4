using Ardalis.Result;
using Rise.Shared.Amenities;

namespace Rise.Client.Amenities;

/// <summary>
/// in memory IAmenityService for the page tests, every answer can be replaced by a test
/// </summary>
public class FakeAmenityService : IAmenityService
{
    public Result<AmenityResponse.Index> IndexAnswer { get; set; } = Result.Success(new AmenityResponse.Index
    {
        Amenities =
        [
            new AmenityDto.Admin
            {
                Id = 1,
                Names = new Dictionary<string, string> { ["en"] = "Bathtub" },
                Icon = "fa-bath",
                IsActive = false,
            },
            new AmenityDto.Admin
            {
                Id = 2,
                Names = new Dictionary<string, string> { ["en"] = "Wi-Fi", ["nl"] = "Wifi" },
                Icon = "fa-wifi",
                IsActive = true,
                RoomTypeNames = ["Deluxe Room", "Standard Room"],
            },
        ],
    });

    public Result<AmenityResponse.Create> CreateAnswer { get; set; } = Result.Success(new AmenityResponse.Create { Id = 3, Name = "Hairdryer" });

    public Result EditAnswer { get; set; } = Result.Success();

    // answer of activate and deactivate
    public Result StatusAnswer { get; set; } = Result.Success();

    public AmenityRequest.Create? LastCreate { get; private set; }
    public AmenityRequest.Edit? LastEdit { get; private set; }
    public List<(int Id, bool IsActive)> StatusChanges { get; } = [];

    public Task<Result<AmenityResponse.Index>> GetIndexAsync(CancellationToken ctx = default) => Task.FromResult(IndexAnswer);

    public Task<Result<AmenityResponse.Create>> CreateAsync(AmenityRequest.Create request, CancellationToken ctx = default)
    {
        LastCreate = request;
        return Task.FromResult(CreateAnswer);
    }

    public Task<Result> EditAsync(AmenityRequest.Edit request, CancellationToken ctx = default)
    {
        LastEdit = request;
        return Task.FromResult(EditAnswer);
    }

    public Task<Result> ActivateAsync(int amenityId, CancellationToken ctx = default)
    {
        StatusChanges.Add((amenityId, true));
        return Task.FromResult(StatusAnswer);
    }

    public Task<Result> DeactivateAsync(int amenityId, CancellationToken ctx = default)
    {
        StatusChanges.Add((amenityId, false));
        return Task.FromResult(StatusAnswer);
    }
}
