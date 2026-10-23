using Rise.Shared.Bookings;
using Rise.Shared.Identity;

namespace Rise.Server.Endpoints.Bookings;

/// <summary>
/// Returns the booking matrix (rooms × date window) for the admin calendar view.
/// </summary>
public class Matrix(IBookingService bookingService)
    : Endpoint<BookingRequest.Matrix, Result<BookingResponse.Matrix>>
{
    public override void Configure()
    {
        Get("/api/bookings/matrix");
        Roles(AppRoles.Administrator);
    }

    public override Task<Result<BookingResponse.Matrix>> ExecuteAsync(
        BookingRequest.Matrix req, CancellationToken ct)
        => bookingService.GetMatrixAsync(req, ct);
}