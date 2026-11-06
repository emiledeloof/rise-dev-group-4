using Rise.Shared.Common;
using Rise.Shared.Rooms;

namespace Rise.Client.Search;

/// <summary>
/// one suggested room with its selected rate in the recommendation combination
/// </summary>
public record RecommendedRoom(RoomDto.Available Room, RateDto.Available Rate);

/// <summary>
/// example combination of rooms that fits the search criteria (Booking.com style)
/// </summary>
public class RecommendedCombination
{
    public required IReadOnlyList<RecommendedRoom> Items { get; init; }

    public int TotalRooms => Items.Count;

    public int TotalCapacity => Items.Sum(x => x.Room.MaxGuests);

    public decimal TotalPrice => Items.Sum(x => x.Rate.TotalPrice);

    /// <summary>
    /// finds the best matching combination of rooms for the given guests and room count
    /// </summary>
    public static RecommendedCombination? FindBest(
        IReadOnlyList<RoomDto.Available>? availableRooms,
        int targetGuests,
        int targetRooms)
    {
        if (availableRooms is null || availableRooms.Count < targetRooms || targetRooms <= 0 || targetGuests <= 0)
            return null;

        // generate all combinations of targetRooms distinct rooms
        var combinations = Combinatorics.GetCombinations(availableRooms, targetRooms);

        // filter combinations that can accommodate all targetGuests and where all rooms have at least 1 rate
        var valid = combinations
            .Where(combo => combo.All(r => r.Rates.Any()))
            .Where(combo => combo.Sum(r => r.MaxGuests) >= targetGuests)
            .Select(combo => new RecommendedCombination
            {
                Items = combo.Select(r => new RecommendedRoom(r, r.Rates.OrderBy(x => x.TotalPrice).First())).ToList()
            })
            .ToList();

        if (valid.Count == 0)
            return null;

        // pick the combination with the lowest total price
        // if prices are tied, prefer the combination with capacity closest to targetGuests (best fit)
        return valid
            .OrderBy(c => c.TotalPrice)
            .ThenBy(c => c.TotalCapacity - targetGuests)
            .First();
    }
}
