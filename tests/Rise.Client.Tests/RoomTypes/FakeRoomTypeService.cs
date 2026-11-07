using Ardalis.Result;
using Rise.Shared.RoomTypes;

namespace Rise.Client.RoomTypes;

/// <summary>
/// in memory IRoomTypeService for the page tests, every answer can be replaced by a test
/// </summary>
public class FakeRoomTypeService : IRoomTypeService
{
    public Result<RoomTypeResponse.Index> IndexAnswer { get; set; } = Result.Success(new RoomTypeResponse.Index
    {
        RoomTypes =
        [
            new RoomTypeDto.Index { Id = 7, Name = "Deluxe Room", Description = "Big bed", RoomNumbers = ["102"] },
        ],
    });

    // deluxe offers wi-fi (active) and the bathtub (inactive), see FakeAmenityService
    public Result<RoomTypeDto.Edit> EditFormAnswer { get; set; } = Result.Success(new RoomTypeDto.Edit
    {
        Id = 7,
        Name = "Deluxe Room",
        Description = "Big bed",
        AmenityIds = [1, 2],
        BedConfigurations = new Dictionary<string, string> { ["en"] = "King bed", ["nl"] = "Kingsize bed" },
    });

    public RoomTypeRequest.Create? LastCreate { get; private set; }
    public RoomTypeRequest.Edit? LastEdit { get; private set; }

    public Task<Result<RoomTypeResponse.Index>> GetIndexAsync(CancellationToken ctx = default) => Task.FromResult(IndexAnswer);

    public Task<Result<RoomTypeDto.Edit>> GetForEditAsync(int roomTypeId, CancellationToken ctx = default) => Task.FromResult(EditFormAnswer);

    public Task<Result<RoomTypeResponse.Create>> CreateAsync(RoomTypeRequest.Create request, CancellationToken ctx = default)
    {
        LastCreate = request;
        return Task.FromResult(Result.Success(new RoomTypeResponse.Create { Id = 8, Name = request.Name }));
    }

    public Task<Result> EditAsync(RoomTypeRequest.Edit request, CancellationToken ctx = default)
    {
        LastEdit = request;
        return Task.FromResult(Result.Success());
    }

    public Task<Result> DeleteAsync(int roomTypeId, CancellationToken ctx = default) => Task.FromResult(Result.Success());
}
