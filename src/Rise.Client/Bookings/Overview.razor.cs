using Microsoft.AspNetCore.Components;
using Rise.Client.Common;
using Rise.Client.I18nText;
using Rise.Shared.Bookings;

namespace Rise.Client.Bookings;

/// <summary>
/// overview of the whole booking, the last step before paying
/// shows what is in the <see cref="BookingDraft"/>, the booking is only saved when the guest clicks "Pay now"
/// </summary>
public partial class Overview : LocalizedComponentBase
{
    // booking the guest put together on the previous pages
    [Inject] public required BookingDraft Draft { get; set; }

    // client BookingService registered in Program.cs, calls the booking endpoint
    [Inject] public required IBookingService BookingService { get; set; }

    [Inject] public required NavigationManager Navigation { get; set; }

    // true while the booking is being saved, shows the spinner and blocks a 2nd click
    private bool _isPaying;

    // msg when a room was booked by someone else meanwhile, null when all is fine
    private string? _conflictMessage;

    // other errors sent back by the api
    private List<string> _errors = [];

    // id of the booking once it is saved, set when only the checkout failed so a retry doesn't save it twice
    private int? _createdBookingId;

    // results page with the dates of the draft, the picked rooms are shown again there
    private string RoomsUrl =>
        $"/search/results?checkIn={Draft.CheckIn:yyyy-MM-dd}&checkOut={Draft.CheckOut:yyyy-MM-dd}&adults={Draft.Adults}";

    /// <summary>
    /// a missing step sends the guest back to it, eg after a page refresh the draft is empty
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        await LoadTextAsync();

        if (!Draft.HasRooms)
            Navigation.NavigateTo("/search");
        else if (Draft.Guest is null)
            Navigation.NavigateTo("/guests/create");
    }

    // saves the booking, then opens the Stripe checkout page
    private async Task PayAsync()
    {
        if (_isPaying)
            return;

        _isPaying = true;
        _conflictMessage = null;
        _errors = [];

        try
        {
            // a retry after a failed checkout reuses the saved booking, saving it again would conflict with itself
            if (_createdBookingId is null)
            {
                var result = await BookingService.CreateAsync(Draft.ToCreateRequest());

                if (!result.IsSuccess)
                {
                    if (result.Status == ResultStatus.Conflict)
                    {
                        _conflictMessage = result.Errors.FirstOrDefault()
                            ?? "One of the rooms is no longer available for these dates.";
                        return;
                    }

                    _errors = ToErrors(result, "Sorry, we could not save your booking. Please try again later.");
                    return;
                }

                _createdBookingId = result.Value.BookingId;
            }

            var checkout = await BookingService.CreateCheckoutSessionAsync(
                new BookingRequest.CreateCheckoutSession { BookingId = _createdBookingId.Value });

            if (!checkout.IsSuccess)
            {
                _errors = ToErrors(checkout, "Your booking is saved but we could not start the payment. Please try again.");
                return;
            }

            // Stripe is another site, forceLoad leaves the app, Stripe sends the guest back to the confirmation page
            Draft.Clear();
            Navigation.NavigateTo(checkout.Value.CheckoutUrl, forceLoad: true);
        }
        finally
        {
            _isPaying = false;
        }
    }

    // validation errors and other errors of the api, the fallback when it sent none
    private static List<string> ToErrors(IResult result, string fallback)
    {
        var errors = result.ValidationErrors.Select(e => e.ErrorMessage)
            .Concat(result.Errors)
            .ToList();

        if (errors.Count == 0)
            errors.Add(fallback);

        return errors;
    }
}
