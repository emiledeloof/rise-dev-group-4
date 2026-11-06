using Rise.Domain.Bookings;
using Rise.Domain.Guests;
using Rise.Domain.Rooms;
using Rise.Domain.Settings;
using Rise.Services.IntegrationTests.Infrastructure;
using Rise.Services.Rooms;
using Rise.Shared.Search;

namespace Rise.Services.IntegrationTests.Rooms;

/// <summary>
/// room search on a real postgres, the in memory tests cannot prove that the overlap query and the rate sort translate to sql
/// </summary>
[Collection(DatabaseCollection.Name)]
public class RoomServiceShould(PostgresFixture database) : IAsyncLifetime
{
    private const string DoubleRoom = "Double Room";
    private const string SingleRoom = "Single Room";

    public Task InitializeAsync() => database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // 2 rooms, the double one is booked from march 10 to march 12
    private async Task ArrangeTwoRoomsWithABookingAsync(bool isVatEnabled = false, decimal vatPercentage = 0m)
    {
        var standard = new RoomType("Standard");

        // the expensive rate first on purpose, the query must sort them cheapest first
        var doubleRoom = new Room("102", DoubleRoom, 2, standard);
        doubleRoom.AddRate("Fully refundable", 139m);
        var doubleRate = doubleRoom.AddRate("Non-refundable", 109m);

        var singleRoom = new Room("101", SingleRoom, 1, standard);
        singleRoom.AddRate("Standard", 79m);

        var guest = new Guest("Jan Janssens", "jan.janssens@example.com", "+32 470 12 34 56",
            new GuestAddress("Koningstraat 12", null, "Belgium"));
        var booking = new Booking("20270301-0001", guest, new DateOnly(2027, 3, 10), new DateOnly(2027, 3, 12), 2);
        booking.AddRoom(doubleRoom, doubleRate);

        await using var dbContext = database.CreateDbContext();
        dbContext.GeneralSettings.Add(new GeneralSettings(isVatEnabled, vatPercentage));
        dbContext.AddRange(standard, doubleRoom, singleRoom, guest, booking);
        await dbContext.SaveChangesAsync();
    }

    private async Task<List<string>> FreeRoomNamesAsync(int checkInDay, int checkOutDay)
    {
        await using var dbContext = database.CreateDbContext();
        var result = await new RoomService(dbContext).GetAvailableAsync(Stay(checkInDay, checkOutDay));

        result.IsSuccess.ShouldBeTrue();
        return result.Value.Rooms.Select(r => r.Name).ToList();
    }

    private static SearchRequest.StayPeriod Stay(int checkInDay, int checkOutDay) => new()
    {
        CheckIn = new DateOnly(2027, 3, checkInDay),
        CheckOut = new DateOnly(2027, 3, checkOutDay),
    };

    [Theory]
    [InlineData(10, 12)] // same nights
    [InlineData(9, 11)]  // ends in the middle
    [InlineData(11, 13)] // starts in the middle
    [InlineData(8, 14)]  // covers the whole booking
    public async Task HideARoomBookedDuringTheStay(int checkInDay, int checkOutDay)
    {
        await ArrangeTwoRoomsWithABookingAsync();

        (await FreeRoomNamesAsync(checkInDay, checkOutDay)).ShouldBe(new[] { SingleRoom });
    }

    [Theory]
    [InlineData(12, 14)] // arrives the day the other guest leaves
    [InlineData(8, 10)]  // leaves the day the other guest arrives
    public async Task ShowARoomWhenTheStayOnlyTouchesTheBooking(int checkInDay, int checkOutDay)
    {
        await ArrangeTwoRoomsWithABookingAsync();

        (await FreeRoomNamesAsync(checkInDay, checkOutDay)).ShouldBe(new[] { DoubleRoom, SingleRoom });
    }

    [Fact]
    public async Task SortTheRatesCheapestFirstAndAddTheVat()
    {
        await ArrangeTwoRoomsWithABookingAsync(isVatEnabled: true, vatPercentage: 10m);

        await using var dbContext = database.CreateDbContext();
        var result = await new RoomService(dbContext).GetAvailableAsync(Stay(20, 22));

        var doubleRoom = result.Value.Rooms.Single(r => r.Name == DoubleRoom);
        // 109 and 139 with 10 % vat, for 2 nights
        doubleRoom.Rates.Select(r => r.PricePerNight).ShouldBe(new[] { 119.90m, 152.90m });
        doubleRoom.TotalPrice.ShouldBe(239.80m);
    }
}
