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

    public Result<RateResponse.Create> CreateAnswer { get; set; } =
        Result.Error("Rate creation is not configured.");

    public List<RateRequest.Index> IndexRequests { get; } = [];

    public List<RateRequest.Create> CreateRequests { get; } = [];

    public Task<Result<RateResponse.Create>> CreateAsync(
        RateRequest.Create request,
        CancellationToken ctx = default)
    {
        CreateRequests.Add(request);

        if (CreateAnswer.IsSuccess)
        {
            var createdRate = CreateAnswer.Value;
            var index = IndexAnswer.Value;
            IndexAnswer = Result.Success(new RateResponse.Index
            {
                Rates =
                [
                    new RateDto.Index
                    {
                        Id = createdRate.Id,
                        Name = createdRate.Name,
                        RoomName = createdRate.RoomName,
                        PricePerNight = createdRate.PricePerNight,
                        Description = createdRate.Description,
                        // a new rate gets the domain defaults
                        MinNights = 1,
                        IsRefundable = true,
                        FullRefundDays = 30,
                        PartialRefundDays = 7,
                        PartialRefundPercent = 50,
                    },
                    .. index.Rates,
                ],
                RoomNames = index.RoomNames.Append(createdRate.RoomName).Distinct().OrderBy(name => name),
                Policies = index.Policies,
                TotalCount = index.TotalCount + 1,
            });
        }

        return Task.FromResult(CreateAnswer);
    }

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
                MinNights = 2,
                IsRefundable = false,
                FullRefundDays = 30,
                PartialRefundDays = 7,
                PartialRefundPercent = 50,
            },
            new RateDto.Index
            {
                Id = 2,
                Name = "Standard",
                RoomName = "Single Room",
                PricePerNight = 79m,
                Description = null,
                MinNights = 1,
                IsRefundable = true,
                FullRefundDays = 30,
                PartialRefundDays = 7,
                PartialRefundPercent = 50,
            },
        ],
        RoomNames = ["Double Room", "Single Room"],
        Policies = [RatePolicies.NonRefundable, RatePolicies.Refundable],
        TotalCount = totalCount,
    };
}
