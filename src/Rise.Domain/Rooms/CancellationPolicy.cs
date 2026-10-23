namespace Rise.Domain.Rooms;

/// <summary>
/// cancellation policy of a <see cref="Rate"/>, says if the guest gets money back when cancelling
/// </summary>
public enum CancellationPolicy
{
    NonRefundable,
    Refundable
}
