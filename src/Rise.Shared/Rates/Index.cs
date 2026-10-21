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