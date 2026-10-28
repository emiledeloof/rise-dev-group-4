using Rise.Shared.Bookings;
using Rise.Shared.Identity;

namespace Rise.Server.Endpoints.Bookings;

/// <summary>
/// Cancels a booking from the admin booking detail page, the refund is only recorded, never executed.
/// </summary>
public sealed class Cancel(IBookingService bookingService)
    : Endpoint<BookingRequest.Cancel, Result<BookingResponse.Cancel>>
{
    public override void Configure()
    {
        Post("/api/bookings/{BookingId}/cancel");
        Roles(AppRoles.Administrator);
    }

    public override Task<Result<BookingResponse.Cancel>> ExecuteAsync(
        BookingRequest.Cancel req,
        CancellationToken ctx)
    {
        return bookingService.CancelAsync(req, ctx);
    }
}
