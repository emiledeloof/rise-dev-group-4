using Rise.Shared.Bookings;
using Rise.Shared.Identity;

namespace Rise.Server.Endpoints.Bookings;

/// <summary>
/// Records that the refund of a cancelled room was paid back by hand, from the admin booking detail page.
/// </summary>
public sealed class RecordRefund(IBookingService bookingService)
    : Endpoint<BookingRequest.RecordRefund, Result<BookingResponse.Refund>>
{
    public override void Configure()
    {
        Post("/api/bookings/{BookingId}/rooms/{BookingRoomId}/refund");
        Roles(AppRoles.Administrator);
    }

    public override Task<Result<BookingResponse.Refund>> ExecuteAsync(
        BookingRequest.RecordRefund req,
        CancellationToken ctx)
    {
        return bookingService.RecordRefundAsync(req, ctx);
    }
}
