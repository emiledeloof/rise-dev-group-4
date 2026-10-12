using Microsoft.AspNetCore.Components;
using Rise.Shared.Bookings;

namespace Rise.Client.Bookings;

/// <summary>
/// overview of the whole booking, the last step before paying
/// shows what is in the <see cref="BookingDraft"/>, the booking is only saved when the guest clicks "Pay now"
/// </summary>
public partial class Overview
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

    // results page with the dates of the draft, the picked rooms are shown again there
    private string RoomsUrl =>
        $"/search/results?checkIn={Draft.CheckIn:yyyy-MM-dd}&checkOut={Draft.CheckOut:yyyy-MM-dd}";

    /// <summary>
    /// a missing step sends the guest back to it, eg after a page refresh the draft is empty
    /// </summary>
    protected override void OnInitialized()
    {
        if (!Draft.HasRooms)
            Navigation.NavigateTo("/search");
        else if (Draft.Guest is null)
            Navigation.NavigateTo("/guests/create");
    }

    // saves the booking, the payment itself comes later
    private async Task PayAsync()
    {
        if (_isPaying)
            return;

        _isPaying = true;
        _conflictMessage = null;
        _errors = [];

        try
        {
            var result = await BookingService.CreateAsync(Draft.ToCreateRequest());

            if (result.IsSuccess)
            {
                // TODO: create the Stripe checkout session and redirect to it instead
                Draft.Clear();
                Navigation.NavigateTo($"/booking/received?number={Uri.EscapeDataString(result.Value.BookingNumber)}");
                return;
            }

            if (result.Status == ResultStatus.Conflict)
            {
                _conflictMessage = result.Errors.FirstOrDefault()
                    ?? "One of the rooms is no longer available for these dates.";
                return;
            }

            _errors = result.ValidationErrors.Select(e => e.ErrorMessage)
                .Concat(result.Errors)
                .ToList();

            if (_errors.Count == 0)
                _errors.Add("Sorry, we could not save your booking. Please try again later.");
        }
        finally
        {
            _isPaying = false;
        }
    }
}
