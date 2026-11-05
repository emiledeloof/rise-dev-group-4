using Ardalis.Result;
using Rise.Client.Pages;
using Rise.Shared.Bookings;
using Rise.Shared.Identity;
using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Bookings;

/// <summary>
/// Tests for the admin booking list page, see Pages/BookingList.razor.
/// Each test follows 3 steps: arrange, act, assert.
/// </summary>
public class BookingListShould : BunitContext
{
    private readonly FakeBookingService _bookingService = new();

    public BookingListShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
        Services.AddSingleton<IBookingService>(_bookingService);
        this.AddAuthorization().SetAuthorized("TEST ADMIN").SetRoles(AppRoles.Administrator);
    }

    [Fact]
    public void ShowARowForEachBooking()
    {
        var cut = Render<BookingList>();
        cut.FindAll("li.booking-row").Count.ShouldBe(3);
    }

    /// <summary>
    /// the payment status stays visible next to the nb of cancelled rooms
    /// </summary>
    [Fact]
    public void ShowThePartialTagNextToTheStatusOfAPartlyCancelledBooking()
    {
        var first = _bookingService.IndexAnswer.Value.Bookings.First();
        first.RoomCount = 2;
        first.CancelledRoomCount = 1;

        var cut = Render<BookingList>();

        var row = cut.FindAll("li.booking-row")[0];
        row.QuerySelector(".booking-list-status")!.TextContent.ShouldContain("Paid");
        row.QuerySelector(".booking-list-partial")!.TextContent.ShouldBe("1 of 2 rooms cancelled");
        cut.FindAll(".booking-list-partial").Count.ShouldBe(1);
    }

    [Fact]
    public void NotShowThePartialTagWhenAllRoomsAreCancelled()
    {
        var booking = _bookingService.IndexAnswer.Value.Bookings.First();
        booking.Status = "Cancelled";
        booking.RoomCount = 2;
        booking.CancelledRoomCount = 2;

        var cut = Render<BookingList>();

        cut.FindAll(".booking-list-partial").ShouldBeEmpty();
        cut.FindAll("li.booking-row")[0].QuerySelector(".booking-list-status")!.TextContent.Trim().ShouldBe("Cancelled");
    }

    [Fact]
    public void ShowGuestNameRoomsDateAndStatusInEachRow()
    {
        var cut = Render<BookingList>();

        // first row (booking 1)
        var firstRow = cut.FindAll("li.booking-row")[0];
        firstRow.QuerySelector(".booking-list-number")!.TextContent.ShouldContain("20261001-SVM-000001");
        firstRow.QuerySelector(".booking-list-guest")!.TextContent.ShouldBe("Guest 1");
        firstRow.QuerySelector(".booking-list-rooms")!.TextContent.ShouldBe("101");
        firstRow.QuerySelector(".booking-list-checkin")!.TextContent.ShouldBe("02 Oct 2026");
        firstRow.QuerySelector(".booking-list-checkout")!.TextContent.ShouldBe("04 Oct 2026");
        firstRow.QuerySelector(".booking-list-status")!.TextContent.Trim().ShouldBe("Paid");
    }

    [Fact]
    public void ShowPaidTagForPaidBooking()
    {
        var cut = Render<BookingList>();
        var firstRow = cut.FindAll("li.booking-row")[0];
        firstRow.QuerySelector(".tag.is-primary")!.TextContent.Trim().ShouldBe("Paid");
    }

    [Fact]
    public void ShowPendingTagForUnpaidBooking()
    {
        var cut = Render<BookingList>();
        var secondRow = cut.FindAll("li.booking-row")[1];
        secondRow.QuerySelector(".tag.is-warning")!.TextContent.Trim().ShouldBe("Pending");
    }

    [Fact]
    public void ShowEmptyMessageWhenNoBookings()
    {
        // arrange — no bookings
        _bookingService.IndexAnswer = Result.Success(new BookingResponse.Index
        {
            Bookings = [],
            TotalCount = 0,
        });

        var cut = Render<BookingList>();

        cut.FindAll("li.booking-row").ShouldBeEmpty();
        cut.Markup.ShouldContain("No bookings yet.");
    }

    [Fact]
    public void ShowErrorWhenServiceFails()
    {
        // arrange
        _bookingService.IndexAnswer = Result.Error("Service unavailable");

        var cut = Render<BookingList>();

        cut.Find(".notification.is-danger").TextContent.ShouldContain("could not be loaded");
    }

    [Fact]
    public void ShowPaginationControlsWhenThereAreMultiplePages()
    {
        // arrange — 25 bookings, page size 10 → 3 pages
        _bookingService.IndexAnswer = Result.Success(new BookingResponse.Index
        {
            Bookings = Enumerable.Range(1, 10).Select(i => new BookingDto.Index
            {
                Id = i,
                BookingNumber = $"20261001-SVM-00000{i}",
                GuestName = $"Guest {i}",
                GuestEmail = $"guest{i}@example.com",
                CheckIn = new DateOnly(2026, 10, 1),
                CheckOut = new DateOnly(2026, 10, 3),
                TotalAmount = 100m,
                RoomNumbers = ["101"],
                Status = "Pending",
                RoomCount = 1,
                CancelledRoomCount = 0,
            }).ToList(),
            TotalCount = 25,
        });

        var cut = Render<BookingList>();

        // 3 page buttons + Previous + Next
        cut.FindAll(".pagination-link").Count.ShouldBe(3);
        cut.Find(".pagination-previous").HasAttribute("disabled").ShouldBeTrue();  // on page 1
        cut.Find(".pagination-next").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void ShowTotalBookingCount()
    {
        var cut = Render<BookingList>();
        cut.Markup.ShouldContain("3 bookings");
    }

    [Fact]
    public void LinkEachRowToTheBookingDetail()
    {
        var cut = Render<BookingList>();

        var links = cut.FindAll("li.booking-row a.booking-list-link");
        links.Count.ShouldBe(3);
        links[0].GetAttribute("href").ShouldBe("admin/bookings/1");
    }
}
