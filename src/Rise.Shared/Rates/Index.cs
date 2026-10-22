using Rise.Shared.Common;
using Rise.Shared.Rooms;

namespace Rise.Shared.Rates;

public static partial class RateRequest
{
    public sealed class Index : QueryRequest.SkipTake
    {
        public string? RoomName { get; set; }
        public string? Policy { get; set; }

        public override string ToString() =>
            $"{base.ToString()}" +
            $"&RoomName={Uri.EscapeDataString(RoomName ?? "")}" +
            $"&Policy={Uri.EscapeDataString(Policy ?? "")}";
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
        public required int TotalCount { get; init; }
    }
}