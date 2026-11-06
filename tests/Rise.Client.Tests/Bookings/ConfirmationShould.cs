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

    // 3 nights, without VAT 109 x 3 = 327, with 20% VAT 130.80 x 3 = 392.40 paid and 327 excl. VAT for information
    private static BookingDto.Detail Booking(bool paid, bool withVat = false) => new()
    {
        Id = 1,
        BookingNumber = "20261012-SVM-000042",
        CheckIn = new DateOnly(2026, 11, 2),
        CheckOut = new DateOnly(2026, 11, 5),
        Guest = new BookingDto.GuestDto { Id = 1, Name = "Marie Peeters", Email = "marie@example.com", Phone = "+32 499 99 99 99" },
        Rooms =
        [
            new BookingDto.BookingRoomDto
            {
                Id = 1, RoomId = 1, RoomNumber = "101", RoomName = "Double Room",
                PricePerNight = withVat ? 130.80m : 109m,
                Total = withVat ? 392.40m : 327m,
                TotalExclVat = withVat ? 327m : null,
            },
        ],
        Payments = [new BookingDto.PaymentDto { Id = 1, Amount = withVat ? 392.40m : 327m, PaidAt = paid ? DateTime.UtcNow : null }],
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

    /// <summary>
    /// with VAT the room shows the price paid labelled incl. VAT and the excl. VAT amount as info only
    /// </summary>
    [Fact]
    public void ShowTheVatInclusivePriceAndTheVatExclusiveAmountAsInfo()
    {
        // arrange
        _bookingService.DetailAnswers.Enqueue(Result.Success(Booking(paid: true, withVat: true)));

        // act
        var cut = RenderWith("cs_test_123");

        // assert
        cut.WaitForAssertion(() => cut.Find(".booking-room-row"));
        cut.Find(".booking-room-name").TextContent.ShouldBe("Double Room");
        cut.Find(".booking-room-price").TextContent.ShouldBe("€392.40");
        cut.Find(".booking-room-price-label").TextContent.ShouldBe("incl. VAT");
        cut.Find(".booking-total-label").TextContent.ShouldBe("Total (incl. VAT)");
        cut.Find(".booking-total-amount").TextContent.ShouldBe("€392.40");
    }

    /// <summary>
    /// without VAT (disabled or 0) there is no excl. VAT amount, no VAT label and no VAT notice
    /// </summary>
    [Fact]
    public void ShowNoVatInfoWhenVatDoesNotApply()
    {
        // arrange
        _bookingService.DetailAnswers.Enqueue(Result.Success(Booking(paid: true)));

        // act
        var cut = RenderWith("cs_test_123");

        // assert
        cut.WaitForAssertion(() => cut.Find(".booking-room-row"));
        cut.Find(".booking-room-price").TextContent.ShouldBe("€327.00");
        cut.FindAll(".booking-room-price-label").ShouldBeEmpty();
        cut.FindAll(".booking-room-excl-vat").ShouldBeEmpty();
        cut.Find(".booking-total-label").TextContent.ShouldBe("Total");
        cut.Find(".booking-total-amount").TextContent.ShouldBe("€327.00");
        cut.Markup.ShouldNotContain("VAT");
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

    /// <summary>
    /// the total is the sum of the rooms, not the payment, which can differ after a refund
    /// </summary>
    [Fact]
    public void ShowTheTotalOfTheRoomsAndNotThePayment()
    {
        // arrange, part of the payment was refunded
        var booking = Booking(paid: true);
        booking.Payments[0].Amount = 100m;
        _bookingService.DetailAnswers.Enqueue(Result.Success(booking));

        // act
        var cut = RenderWith("cs_test_123");

        // assert
        cut.WaitForAssertion(() => cut.Find(".booking-total-amount").TextContent.ShouldBe("€327.00"));
    }
}
