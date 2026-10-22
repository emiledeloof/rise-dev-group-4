using System.Globalization;
using Microsoft.AspNetCore.Components;
using Rise.Shared.RatePricing;

namespace Rise.Client.RatePricing;

/// <summary>
/// admin page to see and change the price per night and the min nb of nights of each rate
/// same idea as Pages/InvoiceList.razor.cs
/// </summary>
public partial class Index
{
    // rates shown in the table, null while the api is still answering
    private IEnumerable<RatePricingDto.Index>? rates;

    // shown instead of the table when the rates cant be loaded
    private string? errorMessage;

    /// <summary>
    /// client service that calls the rate pricing api, given by blazor
    /// </summary>
    [Inject] public required IRatePricingService RatePricingService { get; set; }

    // values of the open form, null when no rate is being edited
    private RatePricingRequest.Edit? editModel;

    // the rate as it was when edit was clicked, compared on save to know what changed
    private RatePricingDto.Index? originalRate;

    // shown under the table after a save that worked
    private string? successMessage;

    // shown in the form when the api refused or failed the save
    private string? saveErrorMessage;

    /// <summary>
    /// loads the rates from the api, called when the page opens and again after each save
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        var result = await RatePricingService.GetIndexAsync();

        // ! empty list on error so the page leaves the loading state and shows the msg
        if (!result.IsSuccess)
        {
            errorMessage = "The rates could not be loaded.";
            rates = [];
            return;
        }

        rates = result.Value.Rates;
    }

    /// <summary>
    /// opens the form for one rate, prefilled with its current values
    /// </summary>
    private void StartEdit(RatePricingDto.Index rate)
    {
        // msgs of a previous save dont belong to this edit
        saveErrorMessage = null;
        successMessage = null;

        // kept to compare with the form on save
        originalRate = rate;

        // * a copy, typing in the form doesnt change the table before the save
        editModel = new RatePricingRequest.Edit
        {
            RateId = rate.RateId,
            PricePerNight = rate.PricePerNight,
            MinNights = rate.MinNights,
            IsRefundable = rate.IsRefundable,
            FullRefundDays = rate.FullRefundDays,
            PartialRefundDays = rate.PartialRefundDays,
            PartialRefundPercent = rate.PartialRefundPercent
        };
    }

    /// <summary>
    /// closes the form without saving
    /// </summary>
    private void CancelEdit()
    {
        editModel = null;
    }

    /// <summary>
    /// sends the form to the api, only called when the validator accepts it
    /// </summary>
    private async Task SaveAsync()
    {
        var result = await RatePricingService.EditAsync(editModel!);

        // form stays open so the admin can retry without typing again
        if (!result.IsSuccess)
        {
            saveErrorMessage = "The changes could not be saved. Please try again.";
            return;
        }

        // ! compared before closing the form, editModel is null after
        var priceChanged = editModel!.PricePerNight != originalRate!.PricePerNight;
        var minNightsChanged = editModel!.MinNights != originalRate!.MinNights;
        // refundable or not, the checkbox
        var refundableChanged = editModel!.IsRefundable != originalRate!.IsRefundable;

        // ! refund values only count for a refundable rate, the service ignores them otherwise
        var fullRefundChanged = editModel!.IsRefundable
            && editModel!.FullRefundDays != originalRate!.FullRefundDays;
        var partialRefundChanged = editModel!.IsRefundable
            && (editModel!.PartialRefundDays != originalRate!.PartialRefundDays
                || editModel!.PartialRefundPercent != originalRate!.PartialRefundPercent);

        // * one msg for every combination, eg saved price and partial refund
        var changes = new List<string>();
        if (priceChanged) changes.Add("price");
        if (minNightsChanged) changes.Add("minimum nights");
        if (refundableChanged) changes.Add(editModel!.IsRefundable ? "cancellation policy (refundable)" : "cancellation policy (non-refundable)");
        if (fullRefundChanged) changes.Add("full refund period");
        if (partialRefundChanged) changes.Add("partial refund");

        successMessage = changes.Count == 0
            ? "Nothing was changed."
            : $"Saved: {JoinWithAnd(changes)}.";

        // close the form and reload so the table shows the saved values
        editModel = null;
        await OnInitializedAsync();
    }

    /// <summary>
    /// price in eur with 2 decimals, eg 175 gives €175.00
    /// </summary>
    private static string FormatPrice(decimal price)
        => $"€{price.ToString("0.00", CultureInfo.InvariantCulture)}";

    /// <summary>
    /// text of the policy for the table, eg 100 pct up to 30 days, 50 pct up to 7 days
    /// </summary>
    private static string FormatPolicy(RatePricingDto.Index rate)
        => rate.IsRefundable
        ? $"100% up to {rate.FullRefundDays} days, {rate.PartialRefundPercent}% up to {rate.PartialRefundDays} days"
        : "Non-refundable";

    /// <summary>
    /// joins the words with commas and a last and, eg price, minimum nights and cancellation policy
    /// </summary>
    private static string JoinWithAnd(List<string> words)
        => words.Count == 1
            ? words[0]
            : $"{string.Join(", ", words.SkipLast(1))} and {words[^1]}";
}
