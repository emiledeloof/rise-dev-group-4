using Rise.Shared.Guests;

namespace Rise.Client.Bookings;

/// <summary>
/// ready made drafts for the booking flow tests
/// </summary>
public static class TestDrafts
{
    // dates are relative to today so the tests keep working whatever day they run
    public static readonly DateOnly CheckIn = DateOnly.FromDateTime(DateTime.Today).AddDays(2);
    public static readonly DateOnly CheckOut = CheckIn.AddDays(3);
    public const int Adults = 2;

    public static DraftRoom DoubleRoom => new(1, "Double Room", 1, "Non-refundable", "No refund after booking", 109m, 327m,
        "Non-refundable, no refund if you cancel");

    public static GuestRequest.Create Guest => new()
    {
        FirstName = "Marie",
        LastName = "Peeters",
        Email = "marie@example.com",
        PhoneNumber = "+32 499 99 99 99",
        AddressLine1 = "Stationsstraat 5",
        AddressLine2 = "Bus 1",
        Country = "Belgium",
    };

    /// <summary>
    /// a draft with the double room picked, with or without guest details
    /// </summary>
    public static BookingDraft WithRooms(bool withGuest = false)
    {
        var draft = new BookingDraft();
        draft.SetRooms(CheckIn, CheckOut, Adults, [DoubleRoom]);
        if (withGuest)
            draft.SetGuest(Guest);
        return draft;
    }
}
