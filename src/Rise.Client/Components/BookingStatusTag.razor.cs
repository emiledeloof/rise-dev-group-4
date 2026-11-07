using Microsoft.AspNetCore.Components;

namespace Rise.Client.Components;

/// <summary>
/// status of a booking as a tag, used in the admin booking list, the booking detail and the final bill
/// </summary>
public partial class BookingStatusTag
{
    /// <summary>
    /// one of BookingStatus, anything else shows as pending
    /// </summary>
    [Parameter, EditorRequired] public required string Status { get; set; }
}
