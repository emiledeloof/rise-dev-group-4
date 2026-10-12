using Ardalis.Result;
using Microsoft.AspNetCore.Components;
using Rise.Shared.Bookings;
using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Bookings;

/// <summary>
/// tests for the page Stripe sends the guest back to, see Bookings/Confirmation.razor
/// </summary>
public class ConfirmationShould : BunitContext
{
    private readonly FakeBookingService _bookingService = new();

    public ConfirmationShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
        Services.AddSingleton<IBookingService>(_bookingService);
    }

    private static BookingDto.Detail Booking(bool paid) => new()
    {
        Id = 1,
        BookingNumber = "20261012-SVM-000042",
        CheckIn = new DateOnly(2026, 11, 2),
        CheckOut = new DateOnly(2026, 11, 5),
        Guest = new BookingDto.GuestDto { Id = 1, Name = "Marie Peeters", Email = "marie@example.com", Phone = "+32 499 99 99 99" },
        Rooms = [new BookingDto.BookingRoomDto { Id = 1, RoomId = 1, RoomNumber = "101", PricePerNight = 109m }],
        Payments = [new BookingDto.PaymentDto { Id = 1, Amount = 327m, PaidAt = paid ? DateTime.UtcNow : null }],
    };

    // opens the page like Stripe does, eg booking/confirmation?session_id=cs_test_123
    private IRenderedComponent<Confirmation> RenderWith(string? sessionId)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(
            sessionId is null ? "booking/confirmation" : $"booking/confirmation?session_id={sessionId}");

        return Render<Confirmation>(p => p.Add(x => x.PollInterval, TimeSpan.Zero));
    }

    [Fact]
    public void ShowThePaidBooking()
    {
        // arrange
        _bookingService.DetailAnswers.Enqueue(Result.Success(Booking(paid: true)));

        // act
        var cut = RenderWith("cs_test_123");

        // assert
        cut.WaitForAssertion(() => cut.Find(".title").TextContent.ShouldContain("confirmed"));
        cut.Find(".confirmation-status").TextContent.ShouldBe("Confirmed");
        cut.Find(".booking-email").TextContent.ShouldContain("marie@example.com");
        _bookingService.DetailRequests.ShouldBe(1);
    }

    [Fact]
    public void ShowAnErrorWhenTheSessionIsMissing()
    {
        // act
        var cut = RenderWith(null);

        // assert
        cut.Find(".notification").TextContent.ShouldContain("missing");
        _bookingService.DetailRequests.ShouldBe(0);
    }

    [Fact]
    public void WaitUntilTheWebhookMarkedThePaymentAsPaid()
    {
        // arrange, the webhook arrives after the guest is back on the page
        _bookingService.DetailAnswers.Enqueue(Result.Success(Booking(paid: false)));
        _bookingService.DetailAnswers.Enqueue(Result.Success(Booking(paid: true)));

        // act
        var cut = RenderWith("cs_test_123");

        // assert
        cut.WaitForAssertion(() => cut.Find(".confirmation-status").TextContent.ShouldBe("Confirmed"));
        _bookingService.DetailRequests.ShouldBe(2);
    }

    [Fact]
    public void StayPendingWhenThePaymentIsNotConfirmedInTime()
    {
        // arrange, the webhook never arrives
        _bookingService.DetailAnswers.Enqueue(Result.Success(Booking(paid: false)));

        // act
        var cut = RenderWith("cs_test_123");

        // assert, the page stops asking and says the email follows later
        cut.WaitForAssertion(() => cut.Find(".booking-email").TextContent.ShouldContain("once the payment is processed"));
        cut.Find(".confirmation-status").TextContent.ShouldBe("Payment processing");
    }
}
