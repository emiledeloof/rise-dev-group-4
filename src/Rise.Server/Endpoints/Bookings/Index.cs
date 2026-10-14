using Rise.Shared.Bookings;
using Rise.Shared.Common;
using Rise.Shared.Identity;

namespace Rise.Server.Endpoints.Bookings;

/// <summary>
/// Returns a paginated list of all bookings for the admin overview.
/// </summary>
public class Index(IBookingService bookingService)
    : Endpoint<QueryRequest.SkipTake, Result<BookingResponse.Index>>
{
    public override void Configure()
    {
        Get("/api/bookings");
        Roles(AppRoles.Administrator);
    }

    public override Task<Result<BookingResponse.Index>> ExecuteAsync(QueryRequest.SkipTake req, CancellationToken ct)
        => bookingService.GetIndexAsync(req, ct);
}
