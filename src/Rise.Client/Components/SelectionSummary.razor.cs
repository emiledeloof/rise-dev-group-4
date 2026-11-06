using Microsoft.AspNetCore.Components;

namespace Rise.Client.Components;

/// <summary>
/// "your selection" box of the results page, the picked rooms, their rate and the total of the stay
/// it only shows what the page gives, the page owns the selection
/// </summary>
public partial class SelectionSummary
{
    /// <summary>
    /// rooms picked by the guest, empty when nothing is picked yet
    /// </summary>
    [Parameter] public IReadOnlyList<SelectedRoom> Rooms { get; set; } = [];

    /// <summary>
    /// nb of nights of the stay, shown under the title
    /// </summary>
    [Parameter] public int Nights { get; set; }

    /// <summary>
    /// called by the continue btn, the page owns the selection so it decides what happens next
    /// </summary>
    [Parameter] public EventCallback OnContinue { get; set; }

    // total of all the picked rooms, the server already computed the total of each one
    // TODO: the booking stories must check the total again on the server before paying
    private decimal Total => Rooms.Sum(r => r.TotalPrice);

    // the server only sends an excl. VAT amount when VAT is enabled and above 0, so then the prices include VAT
    private bool IncludesVat => Rooms.Any(r => r.TotalPriceExclVat is not null);
}
