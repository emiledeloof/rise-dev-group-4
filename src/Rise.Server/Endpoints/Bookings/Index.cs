using Rise.Shared.Bookings;
using Rise.Shared.Identity;

namespace Rise.Server.Endpoints.Bookings;

/// <summary>
/// Returns a paginated list of the bookings for the admin overview, optionally filtered on cancellation.
/// </summary>
public class Index(IBookingService bookingService)
    : Endpoint<BookingRequest.Index, Result<BookingResponse.Index>>
{
    public override void Configure()
    {
        Get("/api/bookings");
        Roles(AppRoles.Administrator);
    }

    public override Task<Result<BookingResponse.Index>> ExecuteAsync(BookingRequest.Index req, CancellationToken ct)
        => bookingService.GetIndexAsync(req, ct);
}
