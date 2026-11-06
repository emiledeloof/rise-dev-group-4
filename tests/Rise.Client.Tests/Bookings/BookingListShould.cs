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

    [Fact]
    public void ShowCancelledTagForCancelledBooking()
    {
        _bookingService.IndexAnswer = Result.Success(AnswerWith(Booking(1, "Cancelled")));

        var cut = Render<BookingList>();

        cut.Find("li.booking-row .tag.is-danger").TextContent.Trim().ShouldBe("Cancelled");
    }

    [Fact]
    public void AskForAllBookingsByDefault()
    {
        var cut = Render<BookingList>();

        _bookingService.IndexRequests.Single().CancellationFilter.ShouldBe(BookingCancellationFilters.All);
        cut.Find(".booking-list-title").TextContent.ShouldBe("All bookings");
    }

    [Theory]
    [InlineData(BookingCancellationFilters.OnlyCancelled, "Cancelled bookings")]
    [InlineData(BookingCancellationFilters.NotCancelled, "Not cancelled bookings")]
    [InlineData(BookingCancellationFilters.All, "All bookings")]
    public async Task SendSelectedFilterToService(string filter, string title)
    {
        var cut = Render<BookingList>();

        await cut.Find(".select-field select").ChangeAsync(filter);

        _bookingService.IndexRequests[^1].CancellationFilter.ShouldBe(filter);
        cut.Find(".booking-list-title").TextContent.ShouldBe(title);
    }

    [Fact]
    public async Task ResetToFirstPageWhenFilterChanges()
    {
        var page = AnswerWith(Enumerable.Range(1, 10).Select(i => Booking(i, "Pending")).ToArray());
        page.TotalCount = 25;
        _bookingService.IndexAnswer = Result.Success(page);
        var cut = Render<BookingList>();

        await cut.Find(".pagination-next").ClickAsync();
        _bookingService.IndexRequests[^1].Skip.ShouldBe(10);

        await cut.Find(".select-field select").ChangeAsync(BookingCancellationFilters.OnlyCancelled);

        _bookingService.IndexRequests[^1].Skip.ShouldBe(0);
    }

    [Fact]
    public async Task ShowCancelledEmptyMessageAndKeepFilterVisible()
    {
        var cut = Render<BookingList>();
        _bookingService.IndexAnswer = Result.Success(AnswerWith());

        await cut.Find(".select-field select").ChangeAsync(BookingCancellationFilters.OnlyCancelled);

        cut.Find(".booking-empty").TextContent.ShouldBe("No cancelled bookings.");
        cut.Find(".select-field select").ShouldNotBeNull();
    }

    [Fact]
    public async Task KeepNewestAnswerWhenAnOlderOneArrivesLate()
    {
        var cut = Render<BookingList>();
        var slowAnswer = new TaskCompletionSource<Result<BookingResponse.Index>>();
        _bookingService.PendingIndexAnswers.Enqueue(slowAnswer.Task);
        _bookingService.IndexAnswer = Result.Success(AnswerWith(Booking(2, "Pending")));

        // act: the first filter is still loading when the admin picks the second
        var slowChange = cut.Find(".select-field select").ChangeAsync(BookingCancellationFilters.OnlyCancelled);
        await cut.Find(".select-field select").ChangeAsync(BookingCancellationFilters.NotCancelled);
        slowAnswer.SetResult(Result.Success(AnswerWith(Booking(1, "Cancelled"))));
        await slowChange;

        // assert
        cut.WaitForAssertion(() => cut.Find(".booking-list-number").TextContent.ShouldEndWith("2"));
        cut.FindAll(".tag.is-danger").ShouldBeEmpty();
    }

    private static BookingResponse.Index AnswerWith(params BookingDto.Index[] bookings) => new()
    {
        Bookings = bookings,
        TotalCount = bookings.Length,
    };

    private static BookingDto.Index Booking(int id, string status) => new()
    {
        Id = id,
        BookingNumber = $"20261001-SVM-00000{id}",
        GuestName = $"Guest {id}",
        GuestEmail = $"guest{id}@example.com",
        CheckIn = new DateOnly(2026, 10, 1),
        CheckOut = new DateOnly(2026, 10, 3),
        TotalAmount = 100m,
        RoomNumbers = ["101"],
        Status = status,
    };
}
