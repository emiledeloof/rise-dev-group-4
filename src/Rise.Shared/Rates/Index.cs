using System.Globalization;
using Rise.Shared.Common;
using Rise.Shared.Rooms;

namespace Rise.Shared.Rates;

public static partial class RateRequest
{
    public sealed class Index : QueryRequest.SkipTake
    {
        public string? RoomName { get; set; }
        public string? Policy { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public bool FilterByDateRange { get; set; }

        public override string ToString()
        {
            var query = $"{base.ToString()}" +
                        $"&RoomName={Uri.EscapeDataString(RoomName ?? "")}" +
                        $"&Policy={Uri.EscapeDataString(Policy ?? "")}";

            if (StartDate is not null)
                query += $"&StartDate={StartDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

            if (EndDate is not null)
                query += $"&EndDate={EndDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

            return query + $"&FilterByDateRange={FilterByDateRange}";
        }
    }
}

/// <summary>
/// values of the policy filter, shared cant see the domain enum so the page sends one of these texts
/// </summary>
public static class RatePolicies
{
    public const string Refundable = "Refundable";
    public const string NonRefundable = "Non-refundable";
}

public static partial class RateResponse
{
    public sealed class Index
    {
        public required IEnumerable<RateDto.Index> Rates { get; init; }
        public required IEnumerable<string> RoomNames { get; init; }
        public required IEnumerable<string> Policies { get; init; }
        public required IEnumerable<string> DateRanges { get; init; }
        public required int TotalCount { get; init; }
    }
}