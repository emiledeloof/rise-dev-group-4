using System.Globalization;
using Microsoft.AspNetCore.Components;
using Rise.Shared.Bookings;

namespace Rise.Client.Bookings;

public partial class Confirmation : ComponentBase
{
    [Parameter]
    [SupplyParameterFromQuery(Name = "session_id")]
    public string? SessionId { get; set; }
    
    private static string FormatAmount(decimal amount)
        => $"€{amount.ToString("0.00", CultureInfo.InvariantCulture)}";   
}