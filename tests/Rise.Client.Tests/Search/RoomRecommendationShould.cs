using Rise.Shared.Rooms;
using Shouldly;

namespace Rise.Client.Search;

/// <summary>
/// Unit tests for <see cref="RecommendedCombination.FindBest"/> (Booking.com style recommendation algorithm).
/// </summary>
public class RoomRecommendationShould
{
    private static RoomDto.Available CreateRoom(int id, string name, int maxGuests, decimal price) => new()
    {
        Id = id,
        Name = name,
        MaxGuests = maxGuests,
        PricePerNight = price / 3,
        Nights = 3,
        TotalPrice = price,
        Rates =
        [
            new RateDto.Available
            {
                Id = id * 10,
                Name = "Standard Rate",
                PricePerNight = price / 3,
                TotalPrice = price,
                IsRefundable = false,
                FullRefundDays = 0,
                PartialRefundDays = 0,
                PartialRefundPercent = 0,
            }
        ]
    };

    [Fact]
    public void RecommendCheapestSingleRoomFittingGuests()
    {
        // Arrange
        var rooms = new List<RoomDto.Available>
        {
            CreateRoom(1, "Deluxe Room", 2, 450m),
            CreateRoom(2, "Standard Room", 2, 300m),
            CreateRoom(3, "Suite", 4, 600m),
        };

        // Act
        var result = RecommendedCombination.FindBest(rooms, targetGuests: 2, targetRooms: 1);

        // Assert
        result.ShouldNotBeNull();
        result.TotalRooms.ShouldBe(1);
        result.Items.Single().Room.Name.ShouldBe("Standard Room");
        result.TotalPrice.ShouldBe(300m);
    }

    [Fact]
    public void RecommendCheapestTwoRoomCombinationFittingGuests()
    {
        // Arrange: 3 rooms
        var rooms = new List<RoomDto.Available>
        {
            CreateRoom(1, "Room A", 2, 200m),
            CreateRoom(2, "Room B", 2, 300m),
            CreateRoom(3, "Room C", 2, 400m),
        };

        // Act: 3 guests across 2 rooms
        var result = RecommendedCombination.FindBest(rooms, targetGuests: 3, targetRooms: 2);

        // Assert: Room A (200) + Room B (300) = 500 total, capacity 4 >= 3
        result.ShouldNotBeNull();
        result.TotalRooms.ShouldBe(2);
        result.TotalPrice.ShouldBe(500m);
        result.Items.Select(x => x.Room.Name).ShouldBe(["Room A", "Room B"]);
    }

    [Fact]
    public void ReturnNullWhenNotEnoughRoomsAvailable()
    {
        // Arrange
        var rooms = new List<RoomDto.Available>
        {
            CreateRoom(1, "Room A", 2, 200m)
        };

        // Act
        var result = RecommendedCombination.FindBest(rooms, targetGuests: 2, targetRooms: 2);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void ReturnNullWhenTotalCapacityCannotFitGuests()
    {
        // Arrange: 2 rooms with capacity 1 each (total 2)
        var rooms = new List<RoomDto.Available>
        {
            CreateRoom(1, "Single A", 1, 100m),
            CreateRoom(2, "Single B", 1, 100m)
        };

        // Act: trying to fit 3 guests in 2 rooms
        var result = RecommendedCombination.FindBest(rooms, targetGuests: 3, targetRooms: 2);

        // Assert
        result.ShouldBeNull();
    }
}
