using Ardalis.Result;
using Rise.Client.Pages;
using Rise.Shared.Bookings;
using Rise.Shared.Identity;
using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Bookings;

public class BookingMatrixShould : BunitContext
{
    private readonly FakeBookingService _bookingService = new();

    public BookingMatrixShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
        Services.AddSingleton<IBookingService>(_bookingService);
        this.AddAuthorization().SetAuthorized("TEST ADMIN").SetRoles(AppRoles.Administrator);
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static DateOnly WindowStart => DateOnly.FromDateTime(DateTime.Today).AddDays(-1);

    private BookingResponse.Matrix SingleRoomMatrix(
        string roomName,
        BookingDto.MatrixBooking[]? bookings = null)
    {
        return new BookingResponse.Matrix
        {
            WindowStart = WindowStart,
            WindowDays = 16,
            Rows =
            [
                new BookingDto.MatrixRow
                {
                    RoomId = 1,
                    RoomNumber = "101",
                    RoomName = roomName,
                    Bookings = [.. bookings ?? []],
                },
            ],
        };
    }

    // ──────────────────────────────────────────────
    // AC1 – status-based colour coding
    // ──────────────────────────────────────────────

    [Fact]
    public void ShowOccupiedCellForPaidBooking()
    {
        _bookingService.MatrixAnswer = Result.Success(SingleRoomMatrix("Suite",
            bookings:
            [
                new BookingDto.MatrixBooking
                {
                    Id = 1,
                    BookingNumber = "20261001-SVM-000001",
                    GuestName = "Anna Paid",
                    CheckIn = WindowStart.AddDays(1),
                    CheckOut = WindowStart.AddDays(4),
                    Status = CellStatus.Occupied,
                },
            ]));

        var cut = Render<BookingMatrix>();

        var occupiedCells = cut.FindAll(".matrix-cell-occupied");
        occupiedCells.Count.ShouldBe(1);
        occupiedCells[0].QuerySelector(".cell-guest-name")!.TextContent.ShouldBe("Anna Paid");
    }

    [Fact]
    public void ShowTemporaryCellForPendingBooking()
    {
        _bookingService.MatrixAnswer = Result.Success(SingleRoomMatrix("Studio",
            bookings:
            [
                new BookingDto.MatrixBooking
                {
                    Id = 2,
                    BookingNumber = "20261001-SVM-000002",
                    GuestName = "Bob Pending",
                    CheckIn = WindowStart.AddDays(2),
                    CheckOut = WindowStart.AddDays(5),
                    Status = CellStatus.Temporary,
                },
            ]));

        var cut = Render<BookingMatrix>();

        var tempCells = cut.FindAll(".matrix-cell-temporary");
        tempCells.Count.ShouldBe(1);
        tempCells[0].QuerySelector(".cell-guest-name")!.TextContent.ShouldBe("Bob Pending");
        cut.FindAll(".matrix-cell-occupied").Count.ShouldBe(0);
    }

    [Fact]
    public void ShowAvailableCellsWhenNoBookingsOverlapWindow()
    {
        _bookingService.MatrixAnswer = Result.Success(SingleRoomMatrix("Deluxe"));

        var cut = Render<BookingMatrix>();

        // All 16 days should collapse into a single available span
        var availableCells = cut.FindAll(".matrix-cell-available");
        availableCells.Count.ShouldBeGreaterThan(0);
        cut.FindAll(".matrix-cell-occupied").Count.ShouldBe(0);
        cut.FindAll(".matrix-cell-temporary").Count.ShouldBe(0);
    }

    [Fact]
    public void OccupiedAndTemporaryCoexistInSameRow()
    {
        _bookingService.MatrixAnswer = Result.Success(SingleRoomMatrix("Double",
            bookings:
            [
                new BookingDto.MatrixBooking
                {
                    Id = 3,
                    BookingNumber = "20261001-SVM-000003",
                    GuestName = "Carol Paid",
                    CheckIn = WindowStart.AddDays(1),
                    CheckOut = WindowStart.AddDays(4),
                    Status = CellStatus.Occupied,
                },
                new BookingDto.MatrixBooking
                {
                    Id = 4,
                    BookingNumber = "20261001-SVM-000004",
                    GuestName = "Dave Pending",
                    CheckIn = WindowStart.AddDays(6),
                    CheckOut = WindowStart.AddDays(9),
                    Status = CellStatus.Temporary,
                },
            ]));

        var cut = Render<BookingMatrix>();

        cut.FindAll(".matrix-cell-occupied").Count.ShouldBe(1);
        cut.FindAll(".matrix-cell-temporary").Count.ShouldBe(1);
        cut.FindAll(".matrix-cell-available").Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void LegendDisplaysAllThreeStatuses()
    {
        _bookingService.MatrixAnswer = Result.Success(SingleRoomMatrix("Standard"));

        var cut = Render<BookingMatrix>();

        cut.Find(".legend-chip.available").TextContent.ShouldBe("Available");
        cut.Find(".legend-chip.occupied").TextContent.ShouldBe("Occupied");
        cut.Find(".legend-chip.temporary").TextContent.ShouldBe("Temporary");
    }

    // ──────────────────────────────────────────────
    // AC2 – empty state
    // ──────────────────────────────────────────────

    [Fact]
    public void ShowsEmptyStateWhenNoRoomsAreConfigured()
    {
        _bookingService.MatrixAnswer = Result.Success(new BookingResponse.Matrix
        {
            WindowStart = WindowStart,
            WindowDays = 16,
            Rows = [],
        });

        var cut = Render<BookingMatrix>();

        cut.Find(".matrix-empty-state").ShouldNotBeNull();
        cut.Find(".empty-state-title").TextContent.ShouldBe("No rooms found");
        // The grid must not be rendered
        cut.FindAll(".matrix-grid").Count.ShouldBe(0);
    }

    // ──────────────────────────────────────────────
    // Hover Popup
    // ──────────────────────────────────────────────

    [Fact]
    public void ShowPopupOnMouseEnterBookingCellAndHideOnMouseLeave()
    {
        _bookingService.MatrixAnswer = Result.Success(SingleRoomMatrix("Suite",
            bookings:
            [
                new BookingDto.MatrixBooking
                {
                    Id = 7,
                    BookingNumber = "20261001-SVM-000001",
                    GuestName = "Anna Paid",
                    CheckIn = WindowStart.AddDays(1),
                    CheckOut = WindowStart.AddDays(4),
                    Status = CellStatus.Occupied,
                },
            ]));
        _bookingService.AdminDetailAnswer = Result.Success(TestAdminDetails.Full());

        var cut = Render<BookingMatrix>();

        // Initially popup is hidden
        cut.FindAll(".booking-popup").Count.ShouldBe(0);

        // Hover over the booking cell
        var bookingCell = cut.Find(".matrix-cell-occupied");
        bookingCell.MouseEnter(new Microsoft.AspNetCore.Components.Web.MouseEventArgs { ClientX = 120, ClientY = 220 });

        // Popup should now be rendered with details
        cut.Find(".booking-popup").ShouldNotBeNull();
        cut.Find(".popup-booking-number").TextContent.ShouldBe("20261004-SVM-000007");

        // Leave hover
        bookingCell.MouseLeave();

        // Popup is hidden again
        cut.FindAll(".booking-popup").Count.ShouldBe(0);
    }

    [Fact]
    public void BookingCellLinksToAdminBookingDetails()
    {
        _bookingService.MatrixAnswer = Result.Success(SingleRoomMatrix("Suite",
            bookings:
            [
                new BookingDto.MatrixBooking
                {
                    Id = 42,
                    BookingNumber = "20261001-SVM-000042",
                    GuestName = "Anna Paid",
                    CheckIn = WindowStart.AddDays(1),
                    CheckOut = WindowStart.AddDays(4),
                    Status = CellStatus.Occupied,
                },
            ]));

        var cut = Render<BookingMatrix>();

        var bookingCell = cut.Find(".matrix-cell-occupied");
        bookingCell.TagName.ShouldBe("A", StringCompareShould.IgnoreCase);
        bookingCell.GetAttribute("href").ShouldBe("admin/bookings/42");
    }
}
