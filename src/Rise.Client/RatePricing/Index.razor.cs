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

        // empty list on error so the page leaves the loading state and shows the msg
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

        // a copy, typing in the form doesnt change the table before the save
        editModel = new RatePricingRequest.Edit
        {
            RateId = rate.RateId,
            PricePerNight = rate.PricePerNight,
            MinNights = rate.MinNights
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

        // compared before closing the form, editModel is null after
        var priceChanged = editModel!.PricePerNight != originalRate!.PricePerNight;
        var minNightsChanged = editModel!.MinNights != originalRate!.MinNights;

        // msg that says exactly what was saved
        if (priceChanged && minNightsChanged)
            successMessage = "The price and minimum nights were saved.";
        else if (priceChanged)
            successMessage = "The price was saved.";
        else if (minNightsChanged)
            successMessage = "The minimum nights were saved.";
        else
            successMessage = "Nothing was changed.";

        // close the form and reload so the table shows the saved values
        editModel = null;
        await OnInitializedAsync();
    }

    /// <summary>
    /// price in eur with 2 decimals, eg 175 gives €175.00
    /// </summary>
    private static string FormatPrice(decimal price)
        => $"€{price.ToString("0.00", CultureInfo.InvariantCulture)}";
}
