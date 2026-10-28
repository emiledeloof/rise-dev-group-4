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
                        IsActive = createdRate.IsActive,
                        Description = createdRate.Description,
                        // a new rate gets the domain defaults
                        MinNights = 1,
                        IsRefundable = true,
                        FullRefundDays = 30,
                        PartialRefundDays = 7,
                        PartialRefundPercent = 50,
                        StartDate = createdRate.StartDate,
                        EndDate = createdRate.EndDate,
                    },
                    .. index.Rates,
                ],
                RoomNames = index.RoomNames.Append(createdRate.RoomName).Distinct().OrderBy(name => name),
                Policies = index.Policies,
                DateRanges = index.DateRanges,
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
                IsActive = false,
                Description = "No refund",
                MinNights = 2,
                IsRefundable = false,
                FullRefundDays = 30,
                PartialRefundDays = 7,
                PartialRefundPercent = 50,
                StartDate = new DateOnly(2026, 10, 1),
                EndDate = new DateOnly(2026, 10, 31),
            },
            new RateDto.Index
            {
                Id = 2,
                Name = "Standard",
                RoomName = "Single Room",
                PricePerNight = 79m,
                IsActive = true,
                Description = null,
                MinNights = 1,
                IsRefundable = true,
                FullRefundDays = 30,
                PartialRefundDays = 7,
                PartialRefundPercent = 50,
                StartDate = new DateOnly(2026, 10, 1),
                EndDate = new DateOnly(2026, 10, 31),
            },
        ],
        RoomNames = ["Double Room", "Single Room"],
        Policies = [RatePolicies.NonRefundable, RatePolicies.Refundable],
        DateRanges = ["2026-10-01 - 2026-10-31"],
        TotalCount = totalCount,
    };
}
