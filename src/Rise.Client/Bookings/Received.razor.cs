using Microsoft.AspNetCore.Components;

namespace Rise.Client.Bookings;

/// <summary>
/// page shown after "Pay now" saved the booking, it only shows the number from the url and loads no data
/// </summary>
public partial class Received
{
    /// <summary>
    /// booking number from the url, eg ?number=20261012-SVM-000042
    /// </summary>
    [SupplyParameterFromQuery] public string? Number { get; set; }
}
