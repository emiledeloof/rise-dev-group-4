using Rise.Shared.Bookings;

namespace Rise.Server.Endpoints.Bookings;

public class Create(IBookingService bookingService)
    : Endpoint<BookingRequest.Create, Result<BookingResponse.Create>>
{
    public override void Configure()
    {
        Post("/api/bookings");
        // guests book without an account
        AllowAnonymous();
    }

    public override Task<Result<BookingResponse.Create>> ExecuteAsync(
        BookingRequest.Create req,
        CancellationToken ctx)
    {
        return bookingService.CreateAsync(req, ctx);
    }
}