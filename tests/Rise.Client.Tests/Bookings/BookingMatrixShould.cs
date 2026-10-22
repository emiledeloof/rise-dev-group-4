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

    [Fact]
    public void NotShowPendingBookingsInMatrixGrid()
    {
        var windowStart = DateOnly.FromDateTime(DateTime.Today).AddDays(-1);
        _bookingService.MatrixAnswer = Result.Success(new BookingResponse.Matrix
        {
            WindowStart = windowStart,
            WindowDays = 16,
            Rows =
            [
                new BookingDto.MatrixRow
                {
                    RoomId = 1,
                    RoomNumber = "101",
                    RoomName = "Single Room",
                    Bookings =
                    [
                        // Pending booking should NOT be rendered as a booked cell
                        new BookingDto.MatrixBooking
                        {
                            Id = 10,
                            BookingNumber = "20261001-SVM-000010",
                            GuestName = "Pending Guest",
                            CheckIn = windowStart.AddDays(1),
                            CheckOut = windowStart.AddDays(3),
                            Status = "Pending",
                        },
                    ],
                },
                new BookingDto.MatrixRow
                {
                    RoomId = 2,
                    RoomNumber = "102",
                    RoomName = "Double Room",
                    Bookings =
                    [
                        // Paid booking SHOULD be rendered
                        new BookingDto.MatrixBooking
                        {
                            Id = 11,
                            BookingNumber = "20261001-SVM-000011",
                            GuestName = "Paid Guest",
                            CheckIn = windowStart.AddDays(2),
                            CheckOut = windowStart.AddDays(5),
                            Status = "Paid",
                        },
                    ],
                },
            ],
        });

        var cut = Render<BookingMatrix>();

        // Only the paid booking should produce a flat-booking-cell
        var bookedCells = cut.FindAll(".flat-booking-cell");
        bookedCells.Count.ShouldBe(1);
        bookedCells[0].QuerySelector(".flat-booking-name")!.TextContent.ShouldBe("Paid Guest");

        // The pending booking guest name should not appear anywhere in the matrix
        cut.Markup.ShouldNotContain("Pending Guest");
    }
}
