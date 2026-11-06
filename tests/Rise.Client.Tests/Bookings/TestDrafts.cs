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

    // same room with 20% VAT, 109 x 3 = 327 excl. VAT, 130.80 x 3 = 392.40 incl. VAT, amounts as sent by the server
    public static DraftRoom DoubleRoomWithVat => new(1, "Double Room", 1, "Non-refundable", "No refund after booking", 130.80m, 392.40m,
        "Non-refundable, no refund if you cancel", 327m);

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
    public static BookingDraft WithRooms(bool withGuest = false, bool withVat = false)
    {
        var draft = new BookingDraft();
        draft.SetRooms(CheckIn, CheckOut, Adults, [withVat ? DoubleRoomWithVat : DoubleRoom]);
        if (withGuest)
            draft.SetGuest(Guest);
        return draft;
    }
}
