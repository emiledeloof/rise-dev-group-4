using Microsoft.AspNetCore.Components;
using Rise.Client.Bookings;
using Rise.Shared.Guests;

namespace Rise.Client.Guests;

/// <summary>
/// Code-behind class for the guest details page (<see cref="Create"/>), a step of the booking flow.
/// The details are kept in the <see cref="BookingDraft"/>; they are only saved with the booking on "Pay now".
/// </summary>
public partial class Create
{
    /// <summary>
    /// The booking the guest is putting together, holds the picked rooms and receives the guest details.
    /// </summary>
    [Inject]
    public required BookingDraft Draft { get; set; }

    /// <summary>
    /// Navigation manager used to go to the next step of the booking.
    /// </summary>
    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    /// <summary>
    /// The form model bound to the EditForm. Validated by FluentValidation.
    /// </summary>
    protected GuestRequest.Create Model { get; set; } = new();

    /// <summary>
    /// True once the guest tried to continue with invalid fields, shows the error summary next to the button.
    /// </summary>
    protected bool IsErrorSummaryVisible { get; set; }

    /// <summary>
    /// Link back to the results page with the dates of the draft, so the picked rooms are shown again.
    /// </summary>
    protected string BackToRoomsUrl =>
        $"/search/results?checkIn={Draft.CheckIn:yyyy-MM-dd}&checkOut={Draft.CheckOut:yyyy-MM-dd}&adults={Draft.Adults}";

    /// <summary>
    /// Sends the guest back to the search when no rooms are picked (e.g. after a page refresh),
    /// otherwise prefills the form with the details entered before.
    /// </summary>
    protected override void OnInitialized()
    {
        if (!Draft.HasRooms)
        {
            NavigationManager.NavigateTo("/search");
            return;
        }

        if (Draft.Guest is { } guest)
        {
            // a copy, so unsaved edits don't change the draft
            Model = new GuestRequest.Create
            {
                FirstName = guest.FirstName,
                LastName = guest.LastName,
                Email = guest.Email,
                PhoneNumber = guest.PhoneNumber,
                AddressLine1 = guest.AddressLine1,
                AddressLine2 = guest.AddressLine2,
                Country = guest.Country,
            };
        }
    }

    /// <summary>
    /// Called when the user submits the form and a FluentValidation rule fails.
    /// </summary>
    protected void ShowErrorSummary()
    {
        IsErrorSummaryVisible = true;
    }

    /// <summary>
    /// Called when the user submits the form and all client-side FluentValidation rules pass.
    /// Stores the details in the draft and continues to the booking overview.
    /// </summary>
    protected void Continue()
    {
        Draft.SetGuest(Model);
        NavigationManager.NavigateTo("/booking/overview");
    }
}
