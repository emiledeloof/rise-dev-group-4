using System.Globalization;
using Microsoft.AspNetCore.Components;
using Rise.Client.Common;
using Rise.Client.I18nText;
using Rise.Shared.Bookings;

namespace Rise.Client.Bookings;

/// <summary>
/// page Stripe sends the guest back to after paying, eg /booking/confirmation?session_id=cs_test_...
/// Stripe calls the webhook around the same time, so the booking can still be unpaid on the first load
/// </summary>
public partial class Confirmation : LocalizedComponentBase, IDisposable
{

    // how many times the page asks again while the webhook hasn't marked the payment as paid
    private const int MaxPollAttempts = 10;

    [Parameter]
    [SupplyParameterFromQuery(Name = "session_id")]
    public string? SessionId { get; set; }

    /// <summary>
    /// wait between two checks of the payment, tests set it to zero
    /// </summary>
    [Parameter] public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);

    [Inject] public required IBookingService BookingService { get; set; }

    // stops the polling when the guest leaves the page
    private readonly CancellationTokenSource _cts = new();

    private BookingDto.Detail? booking;
    private string? errorMessage;

    // true while the page is still waiting for the webhook
    private bool isPolling;

    private decimal paymentAmount => booking?.Payments.FirstOrDefault()?.Amount ?? 0;
    private bool IsPaid => booking?.Payments.Any(x => x.PaidAt is not null) == true;
    private int numberOfNights => booking is null
        ? 0
        : booking.CheckOut.DayNumber - booking.CheckIn.DayNumber;
    private int guestCount => booking?.Rooms.Count ?? 0;
    private string roomName => booking is null
        ? string.Empty
        : string.Join(", ", booking.Rooms.Select(x => x.RoomNumber));

    protected override async Task OnInitializedAsync()
    {
        await LoadTextAsync();

        if (string.IsNullOrWhiteSpace(SessionId))
        {
            errorMessage = "The Stripe Checkout Session is missing from the URL.";
            return;
        }

        var result = await BookingService.GetDetailByCheckoutSessionAsync(SessionId, _cts.Token);
        if (!result.IsSuccess)
        {
            errorMessage = "We could not load your booking details.";
            return;
        }

        booking = result.Value;

        try
        {
            await WaitForPaymentAsync(_cts.Token);
        }
        catch (OperationCanceledException)
        {
            // the guest left the page
        }
    }

    // asks again until the webhook marked the payment as paid, keeps the last booking when a check fails
    private async Task WaitForPaymentAsync(CancellationToken ct)
    {
        isPolling = true;

        for (var attempt = 0; attempt < MaxPollAttempts && !IsPaid; attempt++)
        {
            StateHasChanged();
            await Task.Delay(PollInterval, ct);

            var result = await BookingService.GetDetailByCheckoutSessionAsync(SessionId!, ct);
            if (result.IsSuccess)
                booking = result.Value;
        }

        isPolling = false;
    }

    private static string FormatAmount(decimal amount)
        => $"€{amount.ToString("0.00", CultureInfo.InvariantCulture)}";

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
