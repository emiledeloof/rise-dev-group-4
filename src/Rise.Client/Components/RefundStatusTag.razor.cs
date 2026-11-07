using Microsoft.AspNetCore.Components;

namespace Rise.Client.Components;

/// <summary>
/// refund status of the cancelled part of a booking as a tag, shown next to the <see cref="BookingStatusTag"/>
/// renders nothing while no room is cancelled
/// </summary>
public partial class RefundStatusTag
{
    /// <summary>
    /// one of BookingRefundStatus, None or anything else renders nothing
    /// </summary>
    [Parameter, EditorRequired] public required string Status { get; set; }

    /// <summary>
    /// bigger tag for the header of the booking detail, like the booking status there
    /// </summary>
    [Parameter] public bool IsMedium { get; set; }

    private string SizeClass => IsMedium ? "is-medium" : string.Empty;
}
