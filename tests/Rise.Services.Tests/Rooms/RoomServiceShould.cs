using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Rise.Domain.Bookings;
using Rise.Domain.Guests;
using Rise.Domain.Rooms;
using Rise.Persistence;
using Rise.Services.Rooms;
using Rise.Shared.Rooms;
using Rise.Shared.Search;

namespace Rise.Services.Tests.Rooms;

/// <summary>
/// tests for the availability search of <see cref="RoomService"/>
/// same setup as InvoiceServiceShould, an in memory db filled for each test
/// </summary>
public class RoomServiceShould
{
    // the 2 rooms of every test, the double one is booked from the 10th to the 12th of october
    private const string DoubleRoom = "Double Room";
    private const string SingleRoom = "Single Room";

    /// <summary>
    /// fresh db with 2 rooms and 1 booking of the double room from oct 10 to oct 12
    /// each test gives its own db name so the tests don't share data
    /// </summary>
    private static async Task<ApplicationDbContext> CreateSeededDbContextAsync(string databaseName, Action<Room, Booking>? tweak = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        var dbContext = new ApplicationDbContext(options);

        // the expensive rate is added first on purpose, the service must still sort them cheapest first
        var doubleRoom = new Room("102", DoubleRoom, 2, "Spacious room with a double bed.");
        doubleRoom.AddRate("Fully refundable", 139m, "100% refund until 7 days before");
        var doubleRate = doubleRoom.AddRate("Non-refundable", 109m, "No refund after booking");
        doubleRoom.AddPhoto("https://example.com/double-1.jpg");
        doubleRoom.AddPhoto("https://example.com/double-2.jpg");
        doubleRoom.SizeInSquareMeters = 25;
        doubleRoom.BedType = "Double bed";
        doubleRoom.View = "Garden view";
        doubleRoom.Policies = "A baby cot is available on request.";
        // added out of alphabetical order on purpose, the service must sort them by name
        doubleRoom.AddAmenity(new Amenity("Wi-Fi", "fa-wifi"));
        doubleRoom.AddAmenity(new Amenity("Hairdryer", "fa-wind"));

        // no photo, characteristics or amenities on purpose, to check the dto gets empty values
        var singleRoom = new Room("101", SingleRoom, 1, "Cosy room with a single bed.");
        singleRoom.AddRate("Standard", 79m);

        var guest = new Guest("Jan Janssens", "jan.janssens@example.com", "+32 470 12 34 56",
            new GuestAddress("Koningstraat 12", null, "Belgium"));
        var booking = new Booking("20261001-0001", guest, new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 12));
        booking.AddRoom(doubleRoom, doubleRate);

        // lets a test change the data before saving, eg mark the booking as deleted
        tweak?.Invoke(doubleRoom, booking);

        dbContext.Rooms.AddRange(doubleRoom, singleRoom);
        dbContext.Guests.Add(guest);
        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync();

        return dbContext;
    }

    // shortcut to build the stay we search for
    private static SearchRequest.StayPeriod Stay(int checkInDay, int checkOutDay) => new()
    {
        CheckIn = new DateOnly(2026, 10, checkInDay),
        CheckOut = new DateOnly(2026, 10, checkOutDay),
    };

    // names of the free rooms, easier to check than the full dtos
    private static async Task<List<string>> FreeRoomNamesAsync(ApplicationDbContext dbContext, SearchRequest.StayPeriod stay)
    {
        var result = await new RoomService(dbContext).GetAvailableAsync(stay);
        result.IsSuccess.ShouldBeTrue();
        return result.Value.Rooms.Select(r => r.Name).ToList();
    }

    /// <summary>
    /// stays that overlap the booking of oct 10 to 12, the double room must be hidden
    /// Theory runs the same test once per InlineData line
    /// </summary>
    [Theory]
    [InlineData(10, 12)] // exactly the same nights
    [InlineData(9, 11)]  // starts before, ends in the middle
    [InlineData(11, 13)] // starts in the middle, ends after
    [InlineData(8, 14)]  // covers the whole booking
    [InlineData(11, 12)] // inside the booking
    public async Task HideARoomBookedDuringTheStay(int checkInDay, int checkOutDay)
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync($"{nameof(HideARoomBookedDuringTheStay)}-{checkInDay}-{checkOutDay}");

        // act
        var names = await FreeRoomNamesAsync(dbContext, Stay(checkInDay, checkOutDay));

        // assert, only the single room is left
        names.ShouldBe(new[] { SingleRoom });
    }

    /// <summary>
    /// stays that only touch the booking, same day turnover is allowed
    /// </summary>
    [Theory]
    [InlineData(12, 14)] // arrives the day the other guest leaves
    [InlineData(8, 10)]  // leaves the day the other guest arrives
    [InlineData(20, 22)] // far after
    public async Task ShowARoomWhenTheStayDoesNotOverlap(int checkInDay, int checkOutDay)
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync($"{nameof(ShowARoomWhenTheStayDoesNotOverlap)}-{checkInDay}-{checkOutDay}");

        // act
        var names = await FreeRoomNamesAsync(dbContext, Stay(checkInDay, checkOutDay));

        // assert, both rooms, sorted by name
        names.ShouldBe(new[] { DoubleRoom, SingleRoom });
    }

    /// <summary>
    /// a soft deleted booking does not block the room anymore
    /// </summary>
    [Fact]
    public async Task IgnoreDeletedBookings()
    {
        // arrange, same data but the booking is flagged as deleted
        await using var dbContext = await CreateSeededDbContextAsync(nameof(IgnoreDeletedBookings),
            (_, booking) => booking.IsDeleted = true);

        // act
        var names = await FreeRoomNamesAsync(dbContext, Stay(10, 12));

        // assert
        names.ShouldBe(new[] { DoubleRoom, SingleRoom });
    }

    /// <summary>
    /// a soft deleted room is never shown, even when it's free
    /// </summary>
    [Fact]
    public async Task HideDeletedRooms()
    {
        // arrange, the double room is deleted
        await using var dbContext = await CreateSeededDbContextAsync(nameof(HideDeletedRooms),
            (room, _) => room.IsDeleted = true);

        // act, dates where the double room would be free
        var names = await FreeRoomNamesAsync(dbContext, Stay(20, 22));

        // assert
        names.ShouldBe(new[] { SingleRoom });
    }

    /// <summary>
    /// the dto gets the price of the rate and the first photo
    /// </summary>
    [Fact]
    public async Task FillTheRoomDetails()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(FillTheRoomDetails));

        // act
        var result = await new RoomService(dbContext).GetAvailableAsync(Stay(20, 22));

        // assert, Single finds the one room with that name
        var rooms = result.Value.Rooms.ToList();

        var doubleRoom = rooms.Single(r => r.Name == DoubleRoom);
        doubleRoom.Description.ShouldBe("Spacious room with a double bed.");
        doubleRoom.MaxGuests.ShouldBe(2);
        doubleRoom.PricePerNight.ShouldBe(109m);
        doubleRoom.PhotoUrl.ShouldBe("https://example.com/double-1.jpg");

        // no photo means a null url, the card shows its placeholder then
        rooms.Single(r => r.Name == SingleRoom).PhotoUrl.ShouldBeNull();
    }

    /// <summary>
    /// each room gets the nb of nights and the total of the stay
    /// </summary>
    [Fact]
    public async Task FillTheTotalPriceOfTheStay()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(FillTheTotalPriceOfTheStay));

        // act, oct 20 to 23 is 3 nights
        var result = await new RoomService(dbContext).GetAvailableAsync(Stay(20, 23));

        // assert, 109 x 3 for the double room and 79 x 3 for the single one
        var rooms = result.Value.Rooms.ToList();
        rooms.ShouldAllBe(r => r.Nights == 3);
        rooms.Single(r => r.Name == DoubleRoom).TotalPrice.ShouldBe(327m);
        rooms.Single(r => r.Name == SingleRoom).TotalPrice.ShouldBe(237m);
    }

    /// <summary>
    /// all the rates of a room come back, cheapest first, each with its total for the stay
    /// </summary>
    [Fact]
    public async Task ReturnAllRatesCheapestFirst()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ReturnAllRatesCheapestFirst));

        // act, oct 20 to 23 is 3 nights
        var result = await new RoomService(dbContext).GetAvailableAsync(Stay(20, 23));

        // assert, 2 rates for the double room, sorted even though the expensive one was added first
        var rates = result.Value.Rooms.Single(r => r.Name == DoubleRoom).Rates.ToList();
        rates.Select(r => r.Name).ShouldBe(new[] { "Non-refundable", "Fully refundable" });
        rates[0].Description.ShouldBe("No refund after booking");
        rates[0].TotalPrice.ShouldBe(327m);
        rates[1].TotalPrice.ShouldBe(417m);
    }

    // id the in memory db gave to a room, the tests don't know it in advance
    private static int IdOf(ApplicationDbContext dbContext, string name) =>
        dbContext.Rooms.Single(r => r.Name == name).Id;

    /// <summary>
    /// the detail of a room has its name, description and capacity
    /// </summary>
    [Fact]
    public async Task ReturnTheRoomDetail()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ReturnTheRoomDetail));

        // act
        var result = await new RoomService(dbContext).GetDetailAsync(IdOf(dbContext, DoubleRoom));

        // assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe(DoubleRoom);
        result.Value.Description.ShouldBe("Spacious room with a double bed.");
        result.Value.MaxGuests.ShouldBe(2);
    }

    /// <summary>
    /// an id no room has gives NotFound, no made up room
    /// </summary>
    [Fact]
    public async Task ReturnNotFoundForAnUnknownRoom()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ReturnNotFoundForAnUnknownRoom));

        // act
        var result = await new RoomService(dbContext).GetDetailAsync(9999);

        // assert
        result.Status.ShouldBe(ResultStatus.NotFound);
    }

    /// <summary>
    /// a deleted room is not shown anymore, so NotFound too
    /// </summary>
    [Fact]
    public async Task ReturnNotFoundForADeletedRoom()
    {
        // arrange, the double room is deleted
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ReturnNotFoundForADeletedRoom),
            (room, _) => room.IsDeleted = true);

        // act
        var result = await new RoomService(dbContext).GetDetailAsync(IdOf(dbContext, DoubleRoom));

        // assert
        result.Status.ShouldBe(ResultStatus.NotFound);
    }

    /// <summary>
    /// the detail lists all the photos of the room, in the order they were added
    /// </summary>
    [Fact]
    public async Task ReturnTheRoomPhotosInOrder()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ReturnTheRoomPhotosInOrder));

        // act
        var result = await new RoomService(dbContext).GetDetailAsync(IdOf(dbContext, DoubleRoom));

        // assert
        result.Value.PhotoUrls.ShouldBe(new[] { "https://example.com/double-1.jpg", "https://example.com/double-2.jpg" });
    }

    /// <summary>
    /// a room without photos gets an empty list, no made up photo
    /// </summary>
    [Fact]
    public async Task ReturnNoPhotosForARoomWithout()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ReturnNoPhotosForARoomWithout));

        // act
        var result = await new RoomService(dbContext).GetDetailAsync(IdOf(dbContext, SingleRoom));

        // assert
        result.Value.PhotoUrls.ShouldBeEmpty();
    }

    /// <summary>
    /// the detail has the size, bed and view of the room
    /// </summary>
    [Fact]
    public async Task ReturnTheRoomCharacteristics()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ReturnTheRoomCharacteristics));

        // act
        var result = await new RoomService(dbContext).GetDetailAsync(IdOf(dbContext, DoubleRoom));

        // assert
        result.Value.SizeInSquareMeters.ShouldBe(25);
        result.Value.BedType.ShouldBe("Double bed");
        result.Value.View.ShouldBe("Garden view");
    }

    /// <summary>
    /// a room without characteristics gets nulls, no made up values
    /// </summary>
    [Fact]
    public async Task ReturnNoCharacteristicsForARoomWithout()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ReturnNoCharacteristicsForARoomWithout));

        // act
        var result = await new RoomService(dbContext).GetDetailAsync(IdOf(dbContext, SingleRoom));

        // assert
        result.Value.SizeInSquareMeters.ShouldBeNull();
        result.Value.BedType.ShouldBeNull();
        result.Value.View.ShouldBeNull();
    }

    /// <summary>
    /// the detail has the occupancy, pet and cot rules of the room
    /// </summary>
    [Fact]
    public async Task ReturnTheRoomPolicies()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ReturnTheRoomPolicies));

        // act
        var result = await new RoomService(dbContext).GetDetailAsync(IdOf(dbContext, DoubleRoom));

        // assert
        result.Value.Policies.ShouldBe("A baby cot is available on request.");
    }

    /// <summary>
    /// a room without policies gets null, no made up rules
    /// </summary>
    [Fact]
    public async Task ReturnNoPoliciesForARoomWithout()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ReturnNoPoliciesForARoomWithout));

        // act
        var result = await new RoomService(dbContext).GetDetailAsync(IdOf(dbContext, SingleRoom));

        // assert
        result.Value.Policies.ShouldBeNull();
    }

    /// <summary>
    /// the amenities of the room are sorted by name, with their icon
    /// </summary>
    [Fact]
    public async Task ReturnTheRoomAmenitiesSortedByName()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ReturnTheRoomAmenitiesSortedByName));

        // act
        var result = await new RoomService(dbContext).GetDetailAsync(IdOf(dbContext, DoubleRoom));

        // assert
        result.Value.Amenities.Select(a => a.Name).ShouldBe(new[] { "Hairdryer", "Wi-Fi" });
        result.Value.Amenities[1].Icon.ShouldBe("fa-wifi");
    }

    /// <summary>
    /// a deleted amenity is not shown anymore
    /// </summary>
    [Fact]
    public async Task IgnoreDeletedAmenities()
    {
        // arrange, the hairdryer is deleted
        await using var dbContext = await CreateSeededDbContextAsync(nameof(IgnoreDeletedAmenities),
            (room, _) => room.Amenities.Single(a => a.Name == "Hairdryer").IsDeleted = true);

        // act
        var result = await new RoomService(dbContext).GetDetailAsync(IdOf(dbContext, DoubleRoom));

        // assert
        result.Value.Amenities.ShouldHaveSingleItem().Name.ShouldBe("Wi-Fi");
    }

    /// <summary>
    /// a room without amenities gets an empty list, no made up amenity
    /// </summary>
    [Fact]
    public async Task ReturnNoAmenitiesForARoomWithout()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ReturnNoAmenitiesForARoomWithout));

        // act
        var result = await new RoomService(dbContext).GetDetailAsync(IdOf(dbContext, SingleRoom));

        // assert
        result.Value.Amenities.ShouldBeEmpty();
    }

    /// <summary>
    /// the list has every room with its cheapest rate as from price, a booked room is listed too
    /// </summary>
    [Fact]
    public async Task ListAllRoomsWithTheirCheapestRate()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ListAllRoomsWithTheirCheapestRate));

        // act
        var result = await new RoomService(dbContext).GetListAsync(new RoomRequest.List());

        // assert, 109 and not 139 for the double room, the expensive rate was added first
        result.IsSuccess.ShouldBeTrue();
        var rooms = result.Value.Rooms.ToList();
        rooms.Count.ShouldBe(2);
        rooms.Single(r => r.Name == DoubleRoom).FromPrice.ShouldBe(109m);
        rooms.Single(r => r.Name == DoubleRoom).Description.ShouldBe("Spacious room with a double bed.");
        rooms.Single(r => r.Name == SingleRoom).FromPrice.ShouldBe(79m);
        result.Value.TotalCount.ShouldBe(2);
    }

    /// <summary>
    /// each room of the list has its photos in the order they were added, empty when it has none
    /// </summary>
    [Fact]
    public async Task ListThePhotosOfEachRoomInOrder()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ListThePhotosOfEachRoomInOrder));

        // act
        var result = await new RoomService(dbContext).GetListAsync(new RoomRequest.List());

        // assert
        var rooms = result.Value.Rooms.ToList();
        rooms.Single(r => r.Name == DoubleRoom).PhotoUrls
            .ShouldBe(new[] { "https://example.com/double-1.jpg", "https://example.com/double-2.jpg" });
        rooms.Single(r => r.Name == SingleRoom).PhotoUrls.ShouldBeEmpty();
    }

    /// <summary>
    /// never more rooms than asked, the total still counts all of them
    /// </summary>
    [Fact]
    public async Task ListAtMostTheRequestedNumberOfRooms()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ListAtMostTheRequestedNumberOfRooms));

        // act
        var result = await new RoomService(dbContext).GetListAsync(new RoomRequest.List { Take = 1 });

        // assert
        result.Value.Rooms.Count().ShouldBe(1);
        result.Value.TotalCount.ShouldBe(2);
    }

    /// <summary>
    /// a deleted room is not in the list and not in the total
    /// </summary>
    [Fact]
    public async Task SkipDeletedRoomsInTheList()
    {
        // arrange, the double room is deleted
        await using var dbContext = await CreateSeededDbContextAsync(nameof(SkipDeletedRoomsInTheList),
            (room, _) => room.IsDeleted = true);

        // act
        var result = await new RoomService(dbContext).GetListAsync(new RoomRequest.List());

        // assert
        result.Value.Rooms.Select(r => r.Name).ShouldBe(new[] { SingleRoom });
        result.Value.TotalCount.ShouldBe(1);
    }

    /// <summary>
    /// every rate of the double room needs 5 nights, a 2 night stay hides the room
    /// </summary>
    [Fact]
    public async Task HideARoomWhenTheStayIsShorterThanItsMinNights()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(HideARoomWhenTheStayIsShorterThanItsMinNights),
            (room, _) =>
            {
                foreach (var rate in room.Rates)
                    rate.MinNights = 5;
            });

        // act, oct 20 to 22 is 2 nights
        var names = await FreeRoomNamesAsync(dbContext, Stay(20, 22));

        // assert
        names.ShouldBe(new[] { SingleRoom });
    }

    /// <summary>
    /// only the rate that needs 5 nights is hidden, the other one stays
    /// </summary>
    [Fact]
    public async Task HideOnlyTheRatesThatNeedALongerStay()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(HideOnlyTheRatesThatNeedALongerStay),
            (room, _) => room.Rates.Single(r => r.Name == "Fully refundable").MinNights = 5);

        // act, 2 nights
        var result = await new RoomService(dbContext).GetAvailableAsync(Stay(20, 22));

        // assert
        var rates = result.Value.Rooms.Single(r => r.Name == DoubleRoom).Rates;
        rates.Select(r => r.Name).ShouldBe(new[] { "Non-refundable" });
    }

    /// <summary>
    /// a stay of exactly the min nb of nights is allowed
    /// </summary>
    [Fact]
    public async Task KeepARoomWhenTheStayEqualsItsMinNights()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(KeepARoomWhenTheStayEqualsItsMinNights),
            (room, _) =>
            {
                foreach (var rate in room.Rates)
                    rate.MinNights = 2;
            });

        // act, 2 nights
        var names = await FreeRoomNamesAsync(dbContext, Stay(20, 22));

        // assert
        names.ShouldBe(new[] { DoubleRoom, SingleRoom });
    }
}
