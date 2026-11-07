using Rise.Shared.Bookings;
using Rise.Shared.Identity;

namespace Rise.Server.Endpoints.Bookings;

/// <summary>
/// Undoes a refund payment recorded by mistake, the refund of the room is open again.
/// </summary>
public sealed class UndoRefund(IBookingService bookingService)
    : Endpoint<BookingRequest.UndoRefund, Result<BookingResponse.Refund>>
{
    public override void Configure()
    {
        Delete("/api/bookings/{BookingId}/rooms/{BookingRoomId}/refund");
        Roles(AppRoles.Administrator);
    }

    public override Task<Result<BookingResponse.Refund>> ExecuteAsync(
        BookingRequest.UndoRefund req,
        CancellationToken ctx)
    {
        return bookingService.UndoRefundAsync(req, ctx);
    }
}
