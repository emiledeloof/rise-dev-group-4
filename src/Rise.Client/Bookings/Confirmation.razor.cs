using System.Globalization;
using Microsoft.AspNetCore.Components;
using Rise.Shared.Bookings;

namespace Rise.Client.Bookings;

public partial class Confirmation : ComponentBase
{
    private IEnumerable<BookingDto.Index>? bookings;
    private string? errorMessage;
    
    private static string FormatAmount(decimal amount)
        => $"€{amount.ToString("0.00", CultureInfo.InvariantCulture)}";   
}