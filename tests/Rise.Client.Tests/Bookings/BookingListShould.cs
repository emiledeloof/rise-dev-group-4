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
                RefundStatus = BookingRefundStatus.None,
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

        _bookingService.IndexRequests.Single().Status.ShouldBe(BookingStatusFilters.All);
        cut.Find(".booking-list-title").TextContent.ShouldBe("All bookings");
    }

    [Theory]
    [InlineData(BookingStatusFilters.Cancelled, "Cancelled bookings")]
    [InlineData(BookingStatusFilters.NotCancelled, "Not cancelled bookings")]
    [InlineData(BookingStatusFilters.All, "All bookings")]
    [InlineData(BookingStatusFilters.Pending, "Pending bookings")]
    [InlineData(BookingStatusFilters.Paid, "Paid bookings")]
    [InlineData(BookingStatusFilters.PartiallyCancelled, "Partially cancelled bookings")]
    [InlineData(BookingStatusFilters.RefundPending, "Refund pending bookings")]
    [InlineData(BookingStatusFilters.Refunded, "Refunded bookings")]
    public async Task SendSelectedFilterToService(string filter, string title)
    {
        var cut = Render<BookingList>();

        await ChooseFilterAsync(cut, filter);

        _bookingService.IndexRequests[^1].Status.ShouldBe(filter);
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

        await ChooseFilterAsync(cut, BookingStatusFilters.Cancelled);

        _bookingService.IndexRequests[^1].Skip.ShouldBe(0);
    }

    [Fact]
    public void OfferOnlyTheMainChoicesFirst()
    {
        var cut = Render<BookingList>();

        cut.FindAll(".booking-filter-category option").Select(x => x.TextContent)
            .ShouldBe(["All bookings", "Payment status", "Cancellation status"]);
        cut.FindAll(".booking-filter-status").ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Payment")]
    [InlineData("Cancellation")]
    public async Task ShowTheDetailedStatusesOfTheChosenCategoryWithNothingChosen(string category)
    {
        var cut = Render<BookingList>();

        await cut.Find(".booking-filter-category").ChangeAsync(category);

        var statuses = category == "Payment" ? BookingStatusFilters.PaymentValues : BookingStatusFilters.CancellationValues;
        cut.FindAll(".booking-filter-status option").Select(x => x.GetAttribute("value"))
            .ShouldBe([BookingStatusFilters.All, .. statuses]);
        cut.Find(".booking-filter-status option").TextContent.ShouldBe("Any status");
        _bookingService.IndexRequests[^1].Status.ShouldBe(BookingStatusFilters.All);
    }

    [Fact]
    public async Task ShowAllBookingsWhenAnyStatusIsChosenAgain()
    {
        var cut = Render<BookingList>();
        await ChooseFilterAsync(cut, BookingStatusFilters.Paid);

        await cut.Find(".booking-filter-status").ChangeAsync(BookingStatusFilters.All);

        _bookingService.IndexRequests[^1].Status.ShouldBe(BookingStatusFilters.All);
        cut.Find(".booking-list-title").TextContent.ShouldBe("All bookings");
    }

    [Fact]
    public async Task HideTheDetailedStatusesAndShowAllWhenAllBookingsIsChosenAgain()
    {
        var cut = Render<BookingList>();
        await ChooseFilterAsync(cut, BookingStatusFilters.Refunded);

        await cut.Find(".booking-filter-category").ChangeAsync("All");

        cut.FindAll(".booking-filter-status").ShouldBeEmpty();
        _bookingService.IndexRequests[^1].Status.ShouldBe(BookingStatusFilters.All);
    }

    [Fact]
    public void ShowTheRefundStatusNextToTheBookingStatus()
    {
        var booking = Booking(1, "Paid");
        booking.RoomCount = 2;
        booking.CancelledRoomCount = 1;
        booking.RefundStatus = BookingRefundStatus.RefundPending;
        _bookingService.IndexAnswer = Result.Success(AnswerWith(booking));

        var cut = Render<BookingList>();

        var status = cut.Find(".booking-list-status");
        status.QuerySelector(".tag.is-primary")!.TextContent.ShouldBe("Paid");
        status.QuerySelector(".booking-list-partial")!.TextContent.ShouldBe("1 of 2 rooms cancelled");
        status.QuerySelector(".refund-tag-pending")!.TextContent.Trim().ShouldBe("Refund pending");
    }

    [Fact]
    public async Task ShowTheEmptyMessageOfTheRefundPendingFilter()
    {
        var cut = Render<BookingList>();
        _bookingService.IndexAnswer = Result.Success(AnswerWith());

        await ChooseFilterAsync(cut, BookingStatusFilters.RefundPending);

        cut.Find(".booking-empty").TextContent.ShouldBe("No refund pending bookings.");
    }

    [Fact]
    public async Task ShowCancelledEmptyMessageAndKeepFilterVisible()
    {
        var cut = Render<BookingList>();
        _bookingService.IndexAnswer = Result.Success(AnswerWith());

        await ChooseFilterAsync(cut, BookingStatusFilters.Cancelled);

        cut.Find(".booking-empty").TextContent.ShouldBe("No cancelled bookings.");
        cut.Find(".booking-filter-status").ShouldNotBeNull();
    }

    [Fact]
    public async Task KeepNewestAnswerWhenAnOlderOneArrivesLate()
    {
        var cut = Render<BookingList>();
        await cut.Find(".booking-filter-category").ChangeAsync("Cancellation");
        var slowAnswer = new TaskCompletionSource<Result<BookingResponse.Index>>();
        _bookingService.PendingIndexAnswers.Enqueue(slowAnswer.Task);
        _bookingService.IndexAnswer = Result.Success(AnswerWith(Booking(2, "Pending")));

        // act: the first filter is still loading when the admin picks the second
        var slowChange = cut.Find(".booking-filter-status").ChangeAsync(BookingStatusFilters.Cancelled);
        await cut.Find(".booking-filter-status").ChangeAsync(BookingStatusFilters.NotCancelled);
        slowAnswer.SetResult(Result.Success(AnswerWith(Booking(1, "Cancelled"))));
        await slowChange;

        // assert
        cut.WaitForAssertion(() => cut.Find(".booking-list-number").TextContent.ShouldEndWith("2"));
        cut.FindAll(".tag.is-danger").ShouldBeEmpty();
    }

    /// <summary>
    /// picks the category of the filter in the first select, then the filter itself in the second one
    /// </summary>
    private static async Task ChooseFilterAsync(IRenderedComponent<BookingList> cut, string filter)
    {
        var category = BookingStatusFilters.PaymentValues.Contains(filter) ? "Payment"
            : BookingStatusFilters.CancellationValues.Contains(filter) ? "Cancellation"
            : "All";

        await cut.Find(".booking-filter-category").ChangeAsync(category);
        if (category != "All")
            await cut.Find(".booking-filter-status").ChangeAsync(filter);
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
        RoomCount = 1,
        CancelledRoomCount = status == "Cancelled" ? 1 : 0,
        RefundStatus = status == "Cancelled" ? BookingRefundStatus.NoRefund : BookingRefundStatus.None,
    };
}
