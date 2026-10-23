using Rise.Shared.Bookings;
using Rise.Shared.Identity;

namespace Rise.Server.Endpoints.Bookings;

/// <summary>
/// Returns everything about one booking for the admin booking detail page, holds private guest data so admins only.
/// </summary>
public class Detail(IBookingService bookingService)
    : EndpointWithoutRequest<Result<BookingDto.AdminDetail>>
{
    public override void Configure()
    {
        Get("/api/bookings/{bookingId}");
        Roles(AppRoles.Administrator);
    }

    public override Task<Result<BookingDto.AdminDetail>> ExecuteAsync(CancellationToken ct)
    {
        var bookingId = Route<int>("bookingId");
        return bookingService.GetDetailAsync(bookingId, ct);
    }
}
public sealed class DetailByCheckoutSession(IBookingService bookingService)
    : EndpointWithoutRequest<Result<BookingDto.Detail>>
{
    public override void Configure()
    {
        Get("/api/bookings/checkout-session/{checkoutSessionId}");
        AllowAnonymous();
    }

    public override Task<Result<BookingDto.Detail>> ExecuteAsync(CancellationToken ct)
    {
        var checkoutSessionId = Route<string>("checkoutSessionId") ?? string.Empty;
        return bookingService.GetDetailByCheckoutSessionAsync(checkoutSessionId, ct);
    }
}