namespace Rise.Domain.ExtraServices;

/// <summary>
/// Billing frequency of an <see cref="ExtraService"/>: charged every night of the stay or once per booking.
/// </summary>
public enum ChargeCadence
{
    /// <summary>
    /// Billed for each night of the booking.
    /// </summary>
    PerNight = 0,

    /// <summary>
    /// Billed once for the entire stay/booking.
    /// </summary>
    Once = 1
}
