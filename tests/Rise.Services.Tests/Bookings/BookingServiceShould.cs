using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Rise.Domain.Bookings;
using Rise.Domain.Guests;
using Rise.Domain.Rooms;
using Rise.Persistence;
using Rise.Services.Bookings;
using Rise.Services.Payments;
using Rise.Shared.Bookings;
using Rise.Shared.Common;
using Rise.Shared.Guests;

namespace Rise.Services.Tests.Bookings;

/// <summary>
/// tests for saving a booking with <see cref="BookingService.CreateAsync"/>
/// same setup as RoomServiceShould, an in memory db filled for each test
/// </summary>
public class BookingServiceShould
{
    private const string ExistingEmail = "jan.janssens@example.com";

    /// <summary>
    /// fresh db with a double room (2 rates) and a single room
    /// the double room is booked from oct 10 to oct 12 by an existing guest
    /// </summary>
    private static async Task<ApplicationDbContext> CreateSeededDbContextAsync(string databaseName, Action<Booking>? tweak = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        var dbContext = new ApplicationDbContext(options);

        var doubleRoom = new Room("102", "Double Room", 2);
        var doubleRate = doubleRoom.AddRate("Non-refundable", 109m);
        doubleRoom.AddRate("Fully refundable", 139m);

        var singleRoom = new Room("101", "Single Room", 1);
        singleRoom.AddRate("Standard", 79m);
        singleRoom.AddRate("Flexible", 99m);

        var guest = new Guest("Jan Janssens", ExistingEmail, "+32 470 12 34 56",
            new GuestAddress("Koningstraat 12", null, "Belgium"));
        var booking = new Booking("20261001-SVM-000001", guest, new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 12));
        booking.AddRoom(doubleRoom, doubleRate);

        tweak?.Invoke(booking);

        dbContext.Rooms.AddRange(doubleRoom, singleRoom);
        dbContext.Guests.Add(guest);
        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync();

        return dbContext;
    }

    private static BookingService CreateService(ApplicationDbContext dbContext) =>
        new(dbContext, Substitute.For<IStripePaymentGateway>(), Substitute.For<IConfiguration>());

    private static async Task<(int RoomId, int RateId)> RoomAndRateAsync(ApplicationDbContext dbContext, string roomName, string rateName)
    {
        var room = await dbContext.Rooms.Include(x => x.Rates).SingleAsync(x => x.Name == roomName);
        return (room.Id, room.Rates.Single(x => x.Name == rateName).Id);
    }

    private static BookingRequest.Create Request(int checkInDay, int checkOutDay, string email, params (int RoomId, int RateId)[] rooms) => new()
    {
        CheckIn = new DateOnly(2026, 10, checkInDay),
        CheckOut = new DateOnly(2026, 10, checkOutDay),
        Guest = new GuestRequest.Create
        {
            FirstName = "Marie",
            LastName = "Peeters",
            Email = email,
            PhoneNumber = "+32 499 99 99 99",
            AddressLine1 = "Stationsstraat 5",
            AddressLine2 = "Bus 1",
            Country = "Belgium",
        },
        Rooms = rooms.Select(r => new BookingRequest.Create.RoomSelection { RoomId = r.RoomId, RateId = r.RateId }).ToList(),
    };

    [Fact]
    public async Task CreateBookingWithNewGuest()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(CreateBookingWithNewGuest));
        var single = await RoomAndRateAsync(dbContext, "Single Room", "Standard");

        // act
        var result = await CreateService(dbContext).CreateAsync(Request(20, 23, "marie@example.com", single));

        // assert
        result.Status.ShouldBe(ResultStatus.Created);
        result.Value.BookingNumber.ShouldContain("-SVM-");
        result.Value.TotalAmount.ShouldBe(237m);

        var booking = await dbContext.Bookings
            .Include(x => x.Guest)
            .Include(x => x.Rooms).ThenInclude(x => x.Room)
            .SingleAsync(x => x.Id == result.Value.BookingId);
        booking.Guest.Name.ShouldBe("Marie Peeters");
        booking.Guest.Email.ShouldBe("marie@example.com");
        booking.Rooms.Single().Room.Name.ShouldBe("Single Room");
        (await dbContext.Guests.CountAsync()).ShouldBe(2);
    }

    [Fact]
    public async Task ReuseExistingGuestAndUpdateDetails()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ReuseExistingGuestAndUpdateDetails));
        var single = await RoomAndRateAsync(dbContext, "Single Room", "Standard");

        // act, same email as the seeded guest with spaces around it
        var result = await CreateService(dbContext).CreateAsync(Request(20, 23, $" {ExistingEmail} ", single));

        // assert, no 2nd guest and the details are the latest ones
        result.Status.ShouldBe(ResultStatus.Created);
        var guest = await dbContext.Guests.Include(x => x.Bookings).SingleAsync();
        guest.Name.ShouldBe("Marie Peeters");
        guest.Phone.ShouldBe("+32 499 99 99 99");
        guest.Address.AddressLine1.ShouldBe("Stationsstraat 5");
        guest.Bookings.Count.ShouldBe(2);
    }

    [Fact]
    public async Task UseThePriceOfTheSelectedRate()
    {
        // arrange, the flexible rate is not the first nor the cheapest
        await using var dbContext = await CreateSeededDbContextAsync(nameof(UseThePriceOfTheSelectedRate));
        var flexible = await RoomAndRateAsync(dbContext, "Single Room", "Flexible");

        // act
        var result = await CreateService(dbContext).CreateAsync(Request(20, 22, "marie@example.com", flexible));

        // assert
        result.Value.TotalAmount.ShouldBe(198m);
        var bookingRoom = await dbContext.BookingRooms.Include(x => x.Rate)
            .SingleAsync(x => x.Booking.Id == result.Value.BookingId);
        bookingRoom.Rate.Id.ShouldBe(flexible.RateId);
        bookingRoom.PricePerNight.ShouldBe(99m);
    }

    [Fact]
    public async Task ReturnConflictWhenRoomAlreadyBooked()
    {
        // arrange, the double room is booked from oct 10 to 12
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ReturnConflictWhenRoomAlreadyBooked));
        var doubleRoom = await RoomAndRateAsync(dbContext, "Double Room", "Fully refundable");

        // act
        var result = await CreateService(dbContext).CreateAsync(Request(11, 13, "marie@example.com", doubleRoom));

        // assert, nothing extra saved
        result.Status.ShouldBe(ResultStatus.Conflict);
        result.Errors.Single().ShouldContain("Double Room");
        (await dbContext.Bookings.CountAsync()).ShouldBe(1);
        (await dbContext.Guests.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task IgnoreDeletedBookingsWhenCheckingAvailability()
    {
        // arrange, the booking of the double room is deleted
        await using var dbContext = await CreateSeededDbContextAsync(
            nameof(IgnoreDeletedBookingsWhenCheckingAvailability), booking => booking.IsDeleted = true);
        var doubleRoom = await RoomAndRateAsync(dbContext, "Double Room", "Fully refundable");

        // act
        var result = await CreateService(dbContext).CreateAsync(Request(10, 12, "marie@example.com", doubleRoom));

        // assert
        result.Status.ShouldBe(ResultStatus.Created);
    }

    [Fact]
    public async Task ReturnNotFoundWhenRoomMissing()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ReturnNotFoundWhenRoomMissing));

        // act
        var result = await CreateService(dbContext).CreateAsync(Request(20, 22, "marie@example.com", (999, 1)));

        // assert
        result.Status.ShouldBe(ResultStatus.NotFound);
    }

    [Fact]
    public async Task ReturnInvalidWhenRateNotOfRoom()
    {
        // arrange, a rate of the double room with the single room
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ReturnInvalidWhenRateNotOfRoom));
        var single = await RoomAndRateAsync(dbContext, "Single Room", "Standard");
        var doubleRoom = await RoomAndRateAsync(dbContext, "Double Room", "Fully refundable");

        // act
        var result = await CreateService(dbContext).CreateAsync(Request(20, 22, "marie@example.com", (single.RoomId, doubleRoom.RateId)));

        // assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        (await dbContext.Bookings.CountAsync()).ShouldBe(1);
    }

    // ── GetIndexAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetIndexReturnsAllBookings()
    {
        // arrange — db has 1 seeded booking
        await using var dbContext = await CreateSeededDbContextAsync(nameof(GetIndexReturnsAllBookings));
        var request = new QueryRequest.SkipTake { Skip = 0, Take = 50 };

        // act
        var result = await CreateService(dbContext).GetIndexAsync(request);

        // assert
        result.Status.ShouldBe(ResultStatus.Ok);
        result.Value.TotalCount.ShouldBe(1);
        result.Value.Bookings.Single().BookingNumber.ShouldContain("-SVM-");
    }

    [Fact]
    public async Task GetIndexReturnsPaidStatusWhenPaymentIsPaid()
    {
        // arrange — booking has a paid payment
        await using var dbContext = await CreateSeededDbContextAsync(
            nameof(GetIndexReturnsPaidStatusWhenPaymentIsPaid),
            booking =>
            {
                var payment = booking.AddPayment(327m);
                payment.MarkAsPaid(DateTime.UtcNow);
                payment.SetStripePaymentDetails("pi_test", "https://receipt", "visa", "4242");
            });
        var request = new QueryRequest.SkipTake { Skip = 0, Take = 50 };

        // act
        var result = await CreateService(dbContext).GetIndexAsync(request);

        // assert
        result.Value.Bookings.Single().Status.ShouldBe("Paid");
    }

    [Fact]
    public async Task GetIndexReturnsPendingStatusWhenNoPayment()
    {
        // arrange — seeded booking has no payments
        await using var dbContext = await CreateSeededDbContextAsync(nameof(GetIndexReturnsPendingStatusWhenNoPayment));
        var request = new QueryRequest.SkipTake { Skip = 0, Take = 50 };

        // act
        var result = await CreateService(dbContext).GetIndexAsync(request);

        // assert
        result.Value.Bookings.Single().Status.ShouldBe("Pending");
    }

    [Fact]
    public async Task GetIndexRespectsPagination()
    {
        // arrange — add a second booking
        await using var dbContext = await CreateSeededDbContextAsync(nameof(GetIndexRespectsPagination));
        var single = await RoomAndRateAsync(dbContext, "Single Room", "Standard");
        await CreateService(dbContext).CreateAsync(Request(20, 22, "marie@example.com", single));

        // act — take only 1
        var request = new QueryRequest.SkipTake { Skip = 0, Take = 1 };
        var result = await CreateService(dbContext).GetIndexAsync(request);

        // assert
        result.Value.TotalCount.ShouldBe(2);
        result.Value.Bookings.Count().ShouldBe(1);
    }

    [Fact]
    public async Task GetIndexIncludesRoomNumbers()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(GetIndexIncludesRoomNumbers));
        var request = new QueryRequest.SkipTake { Skip = 0, Take = 50 };

        // act
        var result = await CreateService(dbContext).GetIndexAsync(request);

        // assert — seeded booking has the double room (102)
        result.Value.Bookings.Single().RoomNumbers.ShouldContain("102");
    }

    [Fact]
    public async Task GetIndexReturnsTotalAmountForAllNights()
    {
        // arrange — seeded booking: double room at 109 per night, 10 → 12 Oct = 2 nights
        await using var dbContext = await CreateSeededDbContextAsync(nameof(GetIndexReturnsTotalAmountForAllNights));
        var request = new QueryRequest.SkipTake { Skip = 0, Take = 50 };

        // act
        var result = await CreateService(dbContext).GetIndexAsync(request);

        // assert
        result.Value.Bookings.Single().TotalAmount.ShouldBe(218m);
    }

    // changes the min nb of nights of a rate after the seed
    private static async Task SetMinNightsAsync(ApplicationDbContext dbContext, int rateId, int minNights)
    {
        var rate = await dbContext.Rates.SingleAsync(x => x.Id == rateId);
        rate.MinNights = minNights;
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// the server refuses a stay shorter than the min nights of the rate, even if the search was bypassed
    /// </summary>
    [Fact]
    public async Task RejectAStayShorterThanTheMinNights()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(RejectAStayShorterThanTheMinNights));
        var single = await RoomAndRateAsync(dbContext, "Single Room", "Standard");
        await SetMinNightsAsync(dbContext, single.RateId, 5);

        // act, oct 20 to 22 is 2 nights
        var result = await CreateService(dbContext).CreateAsync(Request(20, 22, "marie@example.com", single));

        // assert, nothing saved
        result.Status.ShouldBe(ResultStatus.Invalid);
        (await dbContext.Bookings.CountAsync()).ShouldBe(1);
    }

    /// <summary>
    /// a stay of exactly the min nb of nights can be booked
    /// </summary>
    [Fact]
    public async Task AcceptAStayOfExactlyTheMinNights()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(AcceptAStayOfExactlyTheMinNights));
        var single = await RoomAndRateAsync(dbContext, "Single Room", "Standard");
        await SetMinNightsAsync(dbContext, single.RateId, 2);

        // act, 2 nights
        var result = await CreateService(dbContext).CreateAsync(Request(20, 22, "marie@example.com", single));

        // assert
        result.Status.ShouldBe(ResultStatus.Created);
    }

    /// <summary>
    /// the admin detail holds the guest, the stay, the rooms with their rate and the payments
    /// </summary>
    [Fact]
    public async Task ReturnAdminDetailWithGuestStayRoomsAndPayments()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ReturnAdminDetailWithGuestStayRoomsAndPayments), booking =>
        {
            booking.AddPayment(218m, "cs_test_1").MarkAsPaid(new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc), "in_1", "INV-1");
        });
        var bookingId = (await dbContext.Bookings.SingleAsync()).Id;

        // act
        var result = await CreateService(dbContext).GetDetailAsync(bookingId);

        // assert
        result.Status.ShouldBe(ResultStatus.Ok);
        var detail = result.Value;
        detail.BookingNumber.ShouldBe("20261001-SVM-000001");
        detail.Status.ShouldBe("Paid");
        detail.CheckIn.ShouldBe(new DateOnly(2026, 10, 10));
        detail.CheckOut.ShouldBe(new DateOnly(2026, 10, 12));

        detail.Guest.Name.ShouldBe("Jan Janssens");
        detail.Guest.Email.ShouldBe(ExistingEmail);
        detail.Guest.Phone.ShouldBe("+32 470 12 34 56");
        detail.Guest.AddressLine1.ShouldBe("Koningstraat 12");
        detail.Guest.AddressLine2.ShouldBeNull();
        detail.Guest.Country.ShouldBe("Belgium");

        var room = detail.Rooms.ShouldHaveSingleItem();
        room.RoomNumber.ShouldBe("102");
        room.RoomName.ShouldBe("Double Room");
        room.RateName.ShouldBe("Non-refundable");

        var payment = detail.Payments.ShouldHaveSingleItem();
        payment.Amount.ShouldBe(218m);
        payment.InvoiceNumber.ShouldBe("INV-1");
    }

    /// <summary>
    /// 2 nights at 109 a night gives 218
    /// </summary>
    [Fact]
    public async Task CalculateNightsAndTotalsOfTheAdminDetail()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(CalculateNightsAndTotalsOfTheAdminDetail));
        var bookingId = (await dbContext.Bookings.SingleAsync()).Id;

        // act
        var result = await CreateService(dbContext).GetDetailAsync(bookingId);

        // assert
        result.Value.Nights.ShouldBe(2);
        result.Value.Rooms.Single().PricePerNight.ShouldBe(109m);
        result.Value.Rooms.Single().Total.ShouldBe(218m);
        result.Value.TotalAmount.ShouldBe(218m);
        result.Value.Status.ShouldBe("Pending");
        result.Value.Payments.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReturnNotFoundForAnUnknownBooking()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ReturnNotFoundForAnUnknownBooking));

        // act
        var result = await CreateService(dbContext).GetDetailAsync(9999);

        // assert
        result.Status.ShouldBe(ResultStatus.NotFound);
    }

    [Fact]
    public async Task ReturnNotFoundForADeletedBooking()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(
            nameof(ReturnNotFoundForADeletedBooking), booking => booking.IsDeleted = true);
        var bookingId = (await dbContext.Bookings.SingleAsync()).Id;

        // act
        var result = await CreateService(dbContext).GetDetailAsync(bookingId);

        // assert
        result.Status.ShouldBe(ResultStatus.NotFound);
    }

    // ── GetMatrixAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMatrixReturnsCorrectCellStatusForPaidAndPendingBookings()
    {
        // arrange — seeded booking (Double Room, Oct 10-12) has no payments → Temporary.
        // Add a second booking (Single Room, Oct 10-12) that is paid → Occupied.
        await using var dbContext = await CreateSeededDbContextAsync(nameof(GetMatrixReturnsCorrectCellStatusForPaidAndPendingBookings));
        var single = await dbContext.Rooms.Include(x => x.Rates).SingleAsync(x => x.Name == "Single Room");
        var guest = await dbContext.Guests.FirstAsync();
        var paidBooking = new Booking("20261001-SVM-000002", guest, new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 12));
        paidBooking.AddRoom(single, single.Rates.First());
        var payment = paidBooking.AddPayment(158m);
        payment.MarkAsPaid(DateTime.UtcNow);
        dbContext.Bookings.Add(paidBooking);
        await dbContext.SaveChangesAsync();

        var request = new BookingRequest.Matrix
        {
            WindowStart = new DateOnly(2026, 10, 9),
            WindowDays = 7,
        };

        // act
        var result = await CreateService(dbContext).GetMatrixAsync(request);

        // assert
        result.Status.ShouldBe(ResultStatus.Ok);
        var matrix = result.Value;
        matrix.Rows.Count.ShouldBe(2);

        // Double Room had only a pending (unpaid) booking → status must be Temporary
        var doubleRow = matrix.Rows.Single(r => r.RoomName == "Double Room");
        doubleRow.Bookings.Count.ShouldBe(1);
        doubleRow.Bookings[0].Status.ShouldBe(CellStatus.Temporary);

        // Single Room had a paid booking → status must be Occupied
        var singleRow = matrix.Rows.Single(r => r.RoomName == "Single Room");
        singleRow.Bookings.Count.ShouldBe(1);
        singleRow.Bookings[0].BookingNumber.ShouldBe("20261001-SVM-000002");
        singleRow.Bookings[0].Status.ShouldBe(CellStatus.Occupied);
    }

    [Fact]
    public async Task GetMatrixReturnsAllRoomsAndIncludesTemporaryBookings()
    {
        // arrange — db has rooms; the seeded booking (Double Room, Oct 10-12) is unpaid → Temporary.
        // The Single Room has no bookings in this window.
        await using var dbContext = await CreateSeededDbContextAsync(nameof(GetMatrixReturnsAllRoomsAndIncludesTemporaryBookings));
        var request = new BookingRequest.Matrix
        {
            WindowStart = new DateOnly(2026, 10, 9),
            WindowDays = 7,
        };

        // act
        var result = await CreateService(dbContext).GetMatrixAsync(request);

        // assert
        result.Status.ShouldBe(ResultStatus.Ok);
        result.Value.Rows.Count.ShouldBe(2);

        // Single Room — no bookings in this window at all
        var singleRow = result.Value.Rows.Single(r => r.RoomName == "Single Room");
        singleRow.Bookings.Count.ShouldBe(0);

        // Double Room — one Temporary booking (the seeded unpaid booking)
        var doubleRow = result.Value.Rows.Single(r => r.RoomName == "Double Room");
        doubleRow.Bookings.Count.ShouldBe(1);
        doubleRow.Bookings[0].Status.ShouldBe(CellStatus.Temporary);
    }
}
