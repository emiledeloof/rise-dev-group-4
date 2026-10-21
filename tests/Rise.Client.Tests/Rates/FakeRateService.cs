using Ardalis.Result;
using Rise.Shared.Common;
using Rise.Shared.Rates;
using Rise.Shared.Rooms;

namespace Rise.Client.Rates;

/// <summary>
/// Fake rate service for the admin rates page tests; records requests without making HTTP calls.
/// </summary>
public class FakeRateService : IRateService
{
    public Result<RateResponse.Index> IndexAnswer { get; set; } = CreateIndexAnswer();

    public List<RateRequest.Index> IndexRequests { get; } = [];

    public Task<Result<RateResponse.Create>> CreateAsync(
        RateRequest.Create request,
        CancellationToken ctx = default)
        => Task.FromException<Result<RateResponse.Create>>(
            new NotSupportedException("The rates page does not create rates."));

    public Task<Result<RateResponse.Index>> GetIndexAsync(
        RateRequest.Index request,
        CancellationToken ctx = default)
    {
        IndexRequests.Add(request);
        return Task.FromResult(IndexAnswer);
    }

    public static RateResponse.Index CreateIndexAnswer(int totalCount = 2) => new()
    {
        Rates =
        [
            new RateDto.Index
            {
                Id = 1,
                Name = "Non-refundable",
                RoomName = "Double Room",
                PricePerNight = 90m,
                Description = "No refund",
            },
            new RateDto.Index
            {
                Id = 2,
                Name = "Standard",
                RoomName = "Single Room",
                PricePerNight = 79m,
                Description = null,
            },
        ],
        RoomNames = ["Double Room", "Single Room"],
        Policies = ["No refund"],
        TotalCount = totalCount,
    };
}
