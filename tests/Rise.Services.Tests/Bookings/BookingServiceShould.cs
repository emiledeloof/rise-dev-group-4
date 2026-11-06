using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Rise.Domain.Bookings;
using Rise.Domain.Guests;
using Rise.Domain.Rooms;
using Rise.Domain.Settings;
using Rise.Persistence;
using Rise.Services.Bookings;
using Rise.Services.Payments;
using Rise.Shared.Bookings;
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
        dbContext.GeneralSettings.Add(new GeneralSettings(false, 0m));

        var doubleRoom = new Room("102", "Double Room", 2, new RoomType("Standard"));
        var doubleRate = doubleRoom.AddRate("Non-refundable", 109m);
        doubleRoom.AddRate("Fully refundable", 139m);

        var singleRoom = new Room("101", "Single Room", 1, new RoomType("Standard"));
        singleRoom.AddRate("Standard", 79m);
        singleRoom.AddRate("Flexible", 99m);

        var guest = new Guest("Jan Janssens", ExistingEmail, "+32 470 12 34 56",
            new GuestAddress("Koningstraat 12", null, "Belgium"));
        var booking = new Booking("20261001-SVM-000001", guest, new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 12), 2);
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
        // 1 guest fits in every room, the capacity tests set their own nb
        Adults = 1,
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
    public async Task IncludeVatInTheSavedBookingPriceAndTotal()
    {
        await using var dbContext = await CreateSeededDbContextAsync(nameof(IncludeVatInTheSavedBookingPriceAndTotal));
        var settings = await dbContext.GeneralSettings.SingleAsync();
        settings.Update(true, 20m);
        await dbContext.SaveChangesAsync();
        var standard = await RoomAndRateAsync(dbContext, "Single Room", "Standard");

        var result = await CreateService(dbContext).CreateAsync(Request(20, 23, "marie@example.com", standard));

        result.Status.ShouldBe(ResultStatus.Created);
        result.Value.TotalAmount.ShouldBe(284.40m);
        var bookingRoom = await dbContext.BookingRooms.Include(x => x.Rate)
            .SingleAsync(room => room.Booking.Id == result.Value.BookingId);
        bookingRoom.PricePerNight.ShouldBe(94.80m);
        bookingRoom.Rate.PricePerNight.ShouldBe(79m);
        bookingRoom.VatPercentage.ShouldBe(20m);
    }

    [Fact]
    public async Task StoreZeroVatWhenVatIsDisabled()
    {
        // the seeded settings have VAT off
        await using var dbContext = await CreateSeededDbContextAsync(nameof(StoreZeroVatWhenVatIsDisabled));
        var standard = await RoomAndRateAsync(dbContext, "Single Room", "Standard");

        var result = await CreateService(dbContext).CreateAsync(Request(20, 23, "marie@example.com", standard));

        var bookingRoom = await dbContext.BookingRooms.SingleAsync(room => room.Booking.Id == result.Value.BookingId);
        bookingRoom.VatPercentage.ShouldBe(0m);
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

    [Fact]
    public async Task ReturnInvalidWhenSelectedRateIsInactive()
    {
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ReturnInvalidWhenSelectedRateIsInactive));
        var single = await RoomAndRateAsync(dbContext, "Single Room", "Standard");
        var rate = await dbContext.Rates.SingleAsync(rate => rate.Id == single.RateId);
        rate.IsActive = false;

        var result = await CreateService(dbContext)
            .CreateAsync(Request(20, 22, "marie@example.com", single));

        result.Status.ShouldBe(ResultStatus.Invalid);
        (await dbContext.Bookings.CountAsync()).ShouldBe(1);
    }

    // ── GetIndexAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetIndexReturnsAllBookings()
    {
        // arrange — db has 1 seeded booking
        await using var dbContext = await CreateSeededDbContextAsync(nameof(GetIndexReturnsAllBookings));
        var request = new BookingRequest.Index { Skip = 0, Take = 50 };

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
        var request = new BookingRequest.Index { Skip = 0, Take = 50 };

        // act
        var result = await CreateService(dbContext).GetIndexAsync(request);

        // assert
        result.Value.Bookings.Single().Status.ShouldBe("Paid");
    }

    [Fact]
    public async Task GetIndexIgnoresDeletedPaymentsForTheStatus()
    {
        // arrange — the only settled payment is deleted
        await using var dbContext = await CreateSeededDbContextAsync(
            nameof(GetIndexIgnoresDeletedPaymentsForTheStatus),
            booking =>
            {
                var payment = booking.AddPayment(327m);
                payment.MarkAsPaid(DateTime.UtcNow);
                payment.IsDeleted = true;
            });
        var request = new BookingRequest.Index { Skip = 0, Take = 50 };

        // act
        var result = await CreateService(dbContext).GetIndexAsync(request);

        // assert
        result.Value.Bookings.Single().Status.ShouldBe("Pending");
    }

    [Fact]
    public async Task GetIndexReturnsPendingStatusWhenNoPayment()
    {
        // arrange — seeded booking has no payments
        await using var dbContext = await CreateSeededDbContextAsync(nameof(GetIndexReturnsPendingStatusWhenNoPayment));
        var request = new BookingRequest.Index { Skip = 0, Take = 50 };

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
        var request = new BookingRequest.Index { Skip = 0, Take = 1 };
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
        var request = new BookingRequest.Index { Skip = 0, Take = 50 };

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
        var request = new BookingRequest.Index { Skip = 0, Take = 50 };

        // act
        var result = await CreateService(dbContext).GetIndexAsync(request);

        // assert
        result.Value.Bookings.Single().TotalAmount.ShouldBe(218m);
    }

    // the seeded booking is cancelled, a second booking stays open
    private static async Task<ApplicationDbContext> CreateCancelledAndOpenBookingAsync(string databaseName)
    {
        var dbContext = await CreateSeededDbContextAsync(databaseName,
            booking => booking.Cancel(DateTime.UtcNow, "Guest asked to cancel by phone", 0m));
        var single = await RoomAndRateAsync(dbContext, "Single Room", "Standard");
        await CreateService(dbContext).CreateAsync(Request(20, 22, "marie@example.com", single));
        return dbContext;
    }

    [Fact]
    public async Task ReturnOnlyCancelledBookings()
    {
        // arrange
        await using var dbContext = await CreateCancelledAndOpenBookingAsync(nameof(ReturnOnlyCancelledBookings));
        var request = new BookingRequest.Index { Skip = 0, Take = 50, CancellationFilter = BookingCancellationFilters.OnlyCancelled };

        // act
        var result = await CreateService(dbContext).GetIndexAsync(request);

        // assert
        result.Value.Bookings.Single().Status.ShouldBe("Cancelled");
    }

    [Fact]
    public async Task ExcludeCancelledBookingsWhenFilteringNotCancelled()
    {
        // arrange
        await using var dbContext = await CreateCancelledAndOpenBookingAsync(nameof(ExcludeCancelledBookingsWhenFilteringNotCancelled));
        var request = new BookingRequest.Index { Skip = 0, Take = 50, CancellationFilter = BookingCancellationFilters.NotCancelled };

        // act
        var result = await CreateService(dbContext).GetIndexAsync(request);

        // assert
        result.Value.Bookings.Single().Status.ShouldBe("Pending");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(BookingCancellationFilters.All)]
    public async Task IncludeCancelledBookingsWithoutFilter(string? filter)
    {
        // arrange
        await using var dbContext = await CreateCancelledAndOpenBookingAsync($"{nameof(IncludeCancelledBookingsWithoutFilter)}-{filter ?? "null"}");
        var request = new BookingRequest.Index { Skip = 0, Take = 50, CancellationFilter = filter };

        // act
        var result = await CreateService(dbContext).GetIndexAsync(request);

        // assert
        result.Value.TotalCount.ShouldBe(2);
        result.Value.Bookings.Select(x => x.Status).ShouldBe(["Pending", "Cancelled"], ignoreOrder: true);
    }

    [Theory]
    [InlineData(BookingCancellationFilters.OnlyCancelled)]
    [InlineData(BookingCancellationFilters.NotCancelled)]
    public async Task CountOnlyFilteredBookings(string filter)
    {
        // arrange
        await using var dbContext = await CreateCancelledAndOpenBookingAsync($"{nameof(CountOnlyFilteredBookings)}-{filter}");
        var request = new BookingRequest.Index { Skip = 0, Take = 50, CancellationFilter = filter };

        // act
        var result = await CreateService(dbContext).GetIndexAsync(request);

        // assert
        result.Value.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task IgnoreDeletedBookingsWhenFilteringOnlyCancelled()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(
            nameof(IgnoreDeletedBookingsWhenFilteringOnlyCancelled),
            booking =>
            {
                booking.Cancel(DateTime.UtcNow, "Guest asked to cancel by phone", 0m);
                booking.IsDeleted = true;
            });
        var request = new BookingRequest.Index { Skip = 0, Take = 50, CancellationFilter = BookingCancellationFilters.OnlyCancelled };

        // act
        var result = await CreateService(dbContext).GetIndexAsync(request);

        // assert
        result.Value.TotalCount.ShouldBe(0);
        result.Value.Bookings.ShouldBeEmpty();
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

    [Fact]
    public async Task RejectABookingOutsideTheSelectedRatesDateRange()
    {
        await using var dbContext = await CreateSeededDbContextAsync(nameof(RejectABookingOutsideTheSelectedRatesDateRange));
        var single = await RoomAndRateAsync(dbContext, "Single Room", "Standard");
        var rate = await dbContext.Rates.SingleAsync(rate => rate.Id == single.RateId);
        rate.StartDate = new DateOnly(2026, 10, 21);
        rate.EndDate = new DateOnly(2026, 10, 31);

        var result = await CreateService(dbContext).CreateAsync(Request(20, 22, "marie@example.com", single));

        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ValidationErrors.ShouldContain(error => error.ErrorMessage.Contains("not available"));
        (await dbContext.Bookings.CountAsync()).ShouldBe(1);

        rate.StartDate = new DateOnly(2026, 10, 20);
        rate.EndDate = new DateOnly(2026, 10, 21);
        var validBoundaryBooking = await CreateService(dbContext).CreateAsync(
            Request(20, 22, "marie@example.com", single));
        validBoundaryBooking.Status.ShouldBe(ResultStatus.Created);
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
        var paidBooking = new Booking("20261001-SVM-000002", guest, new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 12), 2);
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

    // ── CancelAsync ────────────────────────────────────────────────────────────

    private static void Pay(Booking booking, decimal amount = 218m)
        => booking.AddPayment(amount).MarkAsPaid(new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc));

    private static BookingRequest.Cancel CancelRequest(int bookingId, decimal refund = 0m) => new()
    {
        BookingId = bookingId,
        Reason = "Guest asked to cancel by phone",
        RefundAmount = refund,
    };

    /// <summary>
    /// adds a paid booking of the single room (79 a night, refundable) for 2 nights, starting in the given nb of days
    /// </summary>
    private static async Task<int> AddFutureBookingAsync(ApplicationDbContext dbContext, int daysUntilCheckIn, decimal paid = 158m, Action<Rate>? rateAtBookingTime = null)
    {
        var single = await dbContext.Rooms.Include(x => x.Rates).SingleAsync(x => x.Name == "Single Room");
        var guest = await dbContext.Guests.FirstAsync();
        var checkIn = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(daysUntilCheckIn);
        var booking = new Booking("20261001-SVM-000003", guest, checkIn, checkIn.AddDays(2), 1);
        var rate = single.Rates.Single(x => x.Name == "Standard");
        rateAtBookingTime?.Invoke(rate);
        booking.AddRoom(single, rate);
        Pay(booking, paid);
        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync();
        return booking.Id;
    }

    [Fact]
    public async Task CancelABookingAndRecordTheRefund()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(CancelABookingAndRecordTheRefund), booking => Pay(booking));
        var bookingId = (await dbContext.Bookings.SingleAsync()).Id;

        // act
        var result = await CreateService(dbContext).CancelAsync(CancelRequest(bookingId, 100m));

        // assert
        result.Status.ShouldBe(ResultStatus.Ok);
        result.Value.BookingNumber.ShouldBe("20261001-SVM-000001");
        result.Value.RefundAmount.ShouldBe(100m);
        var booking = await dbContext.Bookings.SingleAsync();
        booking.IsCancelled.ShouldBeTrue();
        booking.CancellationReason.ShouldBe("Guest asked to cancel by phone");
        booking.RefundAmount.ShouldBe(100m);
    }

    [Fact]
    public async Task NotTouchStripeWhenCancelling()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(NotTouchStripeWhenCancelling), booking => Pay(booking));
        var bookingId = (await dbContext.Bookings.SingleAsync()).Id;
        var stripe = Substitute.For<IStripePaymentGateway>();

        // act
        await new BookingService(dbContext, stripe, Substitute.For<IConfiguration>()).CancelAsync(CancelRequest(bookingId, 218m));

        // assert, the refund is only recorded, the admin pays it back by hand
        stripe.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task KeepACancelledBookingVisibleWithItsTerms()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(KeepACancelledBookingVisibleWithItsTerms), booking => Pay(booking));
        var bookingId = (await dbContext.Bookings.SingleAsync()).Id;
        var service = CreateService(dbContext);
        await service.CancelAsync(CancelRequest(bookingId, 50m));

        // act
        var detail = await service.GetDetailAsync(bookingId);
        var index = await service.GetIndexAsync(new BookingRequest.Index { Skip = 0, Take = 50 });

        // assert
        detail.Status.ShouldBe(ResultStatus.Ok);
        detail.Value.Status.ShouldBe("Cancelled");
        detail.Value.TotalAmount.ShouldBe(218m);
        detail.Value.Rooms.Single().PricePerNight.ShouldBe(109m);
        detail.Value.Payments.Single().Amount.ShouldBe(218m);
        detail.Value.Cancellation.ShouldNotBeNull();
        detail.Value.Cancellation.Reason.ShouldBe("Guest asked to cancel by phone");
        detail.Value.Cancellation.RefundAmount.ShouldBe(50m);
        index.Value.Bookings.Single().Status.ShouldBe("Cancelled");
    }

    [Fact]
    public async Task ReturnConflictWhenAlreadyCancelled()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ReturnConflictWhenAlreadyCancelled), booking =>
        {
            Pay(booking);
            booking.Cancel(DateTime.UtcNow, "First reason", 10m);
        });
        var bookingId = (await dbContext.Bookings.SingleAsync()).Id;

        // act
        var result = await CreateService(dbContext).CancelAsync(CancelRequest(bookingId, 20m));

        // assert, the 1st cancellation is kept
        result.Status.ShouldBe(ResultStatus.Conflict);
        var booking = await dbContext.Bookings.SingleAsync();
        booking.CancellationReason.ShouldBe("First reason");
        booking.RefundAmount.ShouldBe(10m);
    }

    [Fact]
    public async Task RejectARefundAboveTheAmountPaid()
    {
        // arrange, 218 paid
        await using var dbContext = await CreateSeededDbContextAsync(nameof(RejectARefundAboveTheAmountPaid), booking => Pay(booking));
        var bookingId = (await dbContext.Bookings.SingleAsync()).Id;

        // act
        var result = await CreateService(dbContext).CancelAsync(CancelRequest(bookingId, 218.01m));

        // assert, nothing saved
        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ValidationErrors.Single().ErrorMessage.ShouldContain("218.00");
        (await dbContext.Bookings.SingleAsync()).IsCancelled.ShouldBeFalse();
    }

    [Fact]
    public async Task RejectARefundForAnUnpaidBooking()
    {
        // arrange, nothing paid
        await using var dbContext = await CreateSeededDbContextAsync(nameof(RejectARefundForAnUnpaidBooking));
        var bookingId = (await dbContext.Bookings.SingleAsync()).Id;

        // act
        var result = await CreateService(dbContext).CancelAsync(CancelRequest(bookingId, 1m));

        // assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        (await dbContext.Bookings.SingleAsync()).IsCancelled.ShouldBeFalse();
    }

    [Fact]
    public async Task CancelAnUnpaidBookingWithoutRefund()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(CancelAnUnpaidBookingWithoutRefund));
        var bookingId = (await dbContext.Bookings.SingleAsync()).Id;

        // act
        var result = await CreateService(dbContext).CancelAsync(CancelRequest(bookingId));

        // assert
        result.Status.ShouldBe(ResultStatus.Ok);
        (await dbContext.Bookings.SingleAsync()).IsCancelled.ShouldBeTrue();
    }

    [Fact]
    public async Task ReturnNotFoundWhenCancellingAnUnknownBooking()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ReturnNotFoundWhenCancellingAnUnknownBooking));

        // act
        var result = await CreateService(dbContext).CancelAsync(CancelRequest(9999));

        // assert
        result.Status.ShouldBe(ResultStatus.NotFound);
    }

    [Fact]
    public async Task ReturnNotFoundWhenCancellingADeletedBooking()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(
            nameof(ReturnNotFoundWhenCancellingADeletedBooking), booking => booking.IsDeleted = true);
        var bookingId = (await dbContext.Bookings.SingleAsync()).Id;

        // act
        var result = await CreateService(dbContext).CancelAsync(CancelRequest(bookingId));

        // assert
        result.Status.ShouldBe(ResultStatus.NotFound);
    }

    [Fact]
    public async Task FreeTheRoomsOfACancelledBooking()
    {
        // arrange, the double room is booked from oct 10 to 12, then cancelled
        await using var dbContext = await CreateSeededDbContextAsync(nameof(FreeTheRoomsOfACancelledBooking),
            booking => booking.Cancel(DateTime.UtcNow, "Guest is ill", 0m));
        var doubleRoom = await RoomAndRateAsync(dbContext, "Double Room", "Fully refundable");

        // act
        var result = await CreateService(dbContext).CreateAsync(Request(10, 12, "marie@example.com", doubleRoom));

        // assert
        result.Status.ShouldBe(ResultStatus.Created);
    }

    [Fact]
    public async Task HideCancelledBookingsFromTheMatrix()
    {
        // arrange, a paid booking that is cancelled
        await using var dbContext = await CreateSeededDbContextAsync(nameof(HideCancelledBookingsFromTheMatrix), booking =>
        {
            Pay(booking);
            booking.Cancel(DateTime.UtcNow, "Guest is ill", 0m);
        });

        // act
        var result = await CreateService(dbContext).GetMatrixAsync(new BookingRequest.Matrix
        {
            WindowStart = new DateOnly(2026, 10, 9),
            WindowDays = 7,
        });

        // assert
        result.Value.Rows.ShouldAllBe(r => r.Bookings.Count == 0);
    }

    [Fact]
    public async Task RefuseACheckoutSessionForACancelledBooking()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(RefuseACheckoutSessionForACancelledBooking),
            booking => booking.Cancel(DateTime.UtcNow, "Guest is ill", 0m));
        var bookingId = (await dbContext.Bookings.SingleAsync()).Id;

        // act
        var result = await CreateService(dbContext).CreateCheckoutSessionAsync(
            new BookingRequest.CreateCheckoutSession { BookingId = bookingId });

        // assert
        result.Status.ShouldBe(ResultStatus.Conflict);
    }

    /// <summary>
    /// the refundable rate gives 100 pct from 30 days before arrival, 50 pct from 7 days, nothing after
    /// </summary>
    [Theory]
    [InlineData(60, 158)]
    [InlineData(10, 79)]
    [InlineData(2, 0)]
    public async Task SuggestTheRefundOfTheCancellationPolicy(int daysUntilCheckIn, decimal expected)
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync($"{nameof(SuggestTheRefundOfTheCancellationPolicy)}-{daysUntilCheckIn}");
        var bookingId = await AddFutureBookingAsync(dbContext, daysUntilCheckIn);

        // act
        var result = await CreateService(dbContext).GetDetailAsync(bookingId);

        // assert
        result.Value.PaidAmount.ShouldBe(158m);
        result.Value.SuggestedRefundAmount.ShouldBe(expected);
    }

    [Fact]
    public async Task NeverSuggestMoreThanTheAmountPaid()
    {
        // arrange, only 100 of the 158 paid
        await using var dbContext = await CreateSeededDbContextAsync(nameof(NeverSuggestMoreThanTheAmountPaid));
        var bookingId = await AddFutureBookingAsync(dbContext, 60, paid: 100m);

        // act
        var result = await CreateService(dbContext).GetDetailAsync(bookingId);

        // assert
        result.Value.SuggestedRefundAmount.ShouldBe(100m);
    }

    [Fact]
    public async Task SaveTheNbOfGuests()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(SaveTheNbOfGuests));
        var doubleRoom = await RoomAndRateAsync(dbContext, "Double Room", "Fully refundable");
        var request = Request(20, 22, "marie@example.com", doubleRoom);
        request.Adults = 2;

        // act
        var result = await CreateService(dbContext).CreateAsync(request);

        // assert
        result.Status.ShouldBe(ResultStatus.Created);
        var booking = await dbContext.Bookings.SingleAsync(x => x.Id == result.Value.BookingId);
        booking.Adults.ShouldBe(2);
    }

    /// <summary>
    /// the single room fits 1 guest, the search hides it for 2 guests but the api can be called directly
    /// </summary>
    [Fact]
    public async Task RejectMoreGuestsThanTheRoomsFit()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(RejectMoreGuestsThanTheRoomsFit));
        var single = await RoomAndRateAsync(dbContext, "Single Room", "Standard");
        var request = Request(20, 22, "marie@example.com", single);
        request.Adults = 2;

        // act
        var result = await CreateService(dbContext).CreateAsync(request);

        // assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        (await dbContext.Bookings.CountAsync()).ShouldBe(1);
    }

    /// <summary>
    /// 3 guests fit in the double room plus the single room
    /// </summary>
    [Fact]
    public async Task AcceptGuestsSpreadOverSeveralRooms()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(AcceptGuestsSpreadOverSeveralRooms));
        var doubleRoom = await RoomAndRateAsync(dbContext, "Double Room", "Fully refundable");
        var single = await RoomAndRateAsync(dbContext, "Single Room", "Standard");
        var request = Request(20, 22, "marie@example.com", doubleRoom, single);
        request.Adults = 3;

        // act
        var result = await CreateService(dbContext).CreateAsync(request);

        // assert
        result.Status.ShouldBe(ResultStatus.Created);
    }

    [Fact]
    public async Task ReturnTheNbOfGuestsInTheAdminDetail()
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ReturnTheNbOfGuestsInTheAdminDetail));
        var bookingId = (await dbContext.Bookings.SingleAsync()).Id;

        // act
        var result = await CreateService(dbContext).GetDetailAsync(bookingId);

        // assert
        result.Value.Adults.ShouldBe(2);
    }

    /// <summary>
    /// 100 pct from 30 days before arrival, 50 pct from 7 days, nothing within 7 days, same for the page and the cancel form
    /// </summary>
    [Theory]
    [InlineData(30, 100, 158)]
    [InlineData(29, 50, 79)]
    [InlineData(7, 50, 79)]
    [InlineData(6, 0, 0)]
    public async Task ApplyTheRefundStepsOfTheAgreedPolicy(int daysUntilCheckIn, int expectedPercent, decimal expectedRefund)
    {
        // arrange
        await using var dbContext = await CreateSeededDbContextAsync($"{nameof(ApplyTheRefundStepsOfTheAgreedPolicy)}-{daysUntilCheckIn}");
        var bookingId = await AddFutureBookingAsync(dbContext, daysUntilCheckIn);

        // act
        var result = await CreateService(dbContext).GetDetailAsync(bookingId);

        // assert
        var detail = result.Value;
        var room = detail.Rooms.ShouldHaveSingleItem();
        room.IsRefundable.ShouldBeTrue();
        room.FullRefundUntil.ShouldBe(detail.CheckIn.AddDays(-30));
        room.PartialRefundUntil.ShouldBe(detail.CheckIn.AddDays(-7));
        room.RefundPercent.ShouldBe(expectedPercent);
        room.RefundAmount.ShouldBe(expectedRefund);
        detail.RefundReferenceDate.ShouldBe(DateOnly.FromDateTime(DateTime.UtcNow));
        detail.Rooms.Sum(x => x.RefundAmount).ShouldBe(detail.SuggestedRefundAmount);
    }

    [Fact]
    public async Task ShowNoRefundForANonRefundableRate()
    {
        // arrange, far enough ahead for a full refund on a refundable rate
        await using var dbContext = await CreateSeededDbContextAsync(nameof(ShowNoRefundForANonRefundableRate));
        var bookingId = await AddFutureBookingAsync(dbContext, 60,
            rateAtBookingTime: rate => rate.CancellationPolicy = CancellationPolicy.NonRefundable);

        // act
        var result = await CreateService(dbContext).GetDetailAsync(bookingId);

        // assert
        var room = result.Value.Rooms.ShouldHaveSingleItem();
        room.IsRefundable.ShouldBeFalse();
        room.FullRefundUntil.ShouldBeNull();
        room.PartialRefundUntil.ShouldBeNull();
        room.RefundPercent.ShouldBe(0);
        room.RefundAmount.ShouldBe(0m);
        result.Value.SuggestedRefundAmount.ShouldBe(0m);
    }

    /// <summary>
    /// the admin changes the rate after the guest booked, the booking keeps the name and policy agreed at booking time
    /// </summary>
    [Fact]
    public async Task KeepTheAgreedRateAndPolicyWhenTheRateChangesLater()
    {
        // arrange, 10 days ahead gives 50 pct on the agreed policy
        await using var dbContext = await CreateSeededDbContextAsync(nameof(KeepTheAgreedRateAndPolicyWhenTheRateChangesLater));
        var bookingId = await AddFutureBookingAsync(dbContext, 10);
        var rate = (await dbContext.Rooms.Include(x => x.Rates).SingleAsync(x => x.Name == "Single Room")).Rates.Single(x => x.Name == "Standard");
        rate.Name = "Saver";
        rate.CancellationPolicy = CancellationPolicy.NonRefundable;
        rate.FullRefundDays = 60;
        rate.PartialRefundDays = 14;
        rate.PartialRefundPercent = 25;
        await dbContext.SaveChangesAsync();

        // act
        var result = await CreateService(dbContext).GetDetailAsync(bookingId);

        // assert
        var room = result.Value.Rooms.ShouldHaveSingleItem();
        room.RateName.ShouldBe("Standard");
        room.IsRefundable.ShouldBeTrue();
        room.FullRefundDays.ShouldBe(30);
        room.PartialRefundDays.ShouldBe(7);
        room.PartialRefundPercent.ShouldBe(50);
        room.RefundPercent.ShouldBe(50);
        room.RefundAmount.ShouldBe(79m);
    }

    /// <summary>
    /// cancelled 10 days before arrival, the page shows the 50 pct of that day, not the 0 pct of today
    /// </summary>
    [Fact]
    public async Task UseTheCancellationDateForTheRefundOfACancelledBooking()
    {
        // arrange, check-in was 2 days ago so today would give nothing
        await using var dbContext = await CreateSeededDbContextAsync(nameof(UseTheCancellationDateForTheRefundOfACancelledBooking));
        var bookingId = await AddFutureBookingAsync(dbContext, -2);
        var booking = await dbContext.Bookings.Include(x => x.Payments).SingleAsync(x => x.Id == bookingId);
        var cancelledOn = booking.CheckIn.AddDays(-10);
        booking.Cancel(cancelledOn.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc), "Guest asked to cancel by phone", 79m);
        await dbContext.SaveChangesAsync();

        // act
        var result = await CreateService(dbContext).GetDetailAsync(bookingId);

        // assert
        result.Status.ShouldBe(ResultStatus.Ok);
        result.Value.RefundReferenceDate.ShouldBe(cancelledOn);
        var room = result.Value.Rooms.ShouldHaveSingleItem();
        room.RefundPercent.ShouldBe(50);
        room.RefundAmount.ShouldBe(79m);
    }
}
