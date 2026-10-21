using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Rise.Shared.RatePricing;

namespace Rise.Client.RatePricing;

/// <summary>
/// admin page to see and change the price per night of each rate
/// same idea as Pages/InvoiceList.razor.cs
/// </summary>
public partial class Index
{
    // list of rates shown in the table, null while loading
    // type IEnumerable of RatePricingDto.Index with a ? since it can be null
    private IEnumerable<RatePricingDto.Index>? rates;

    // error msg shown when the list cant be loaded, string with a ?
    private string? errorMessage;

    // the service that calls the api, given by blazor with [Inject]
    // same line as IInvoiceService in InvoiceList.razor.cs
    [Inject] public required IRatePricingService RatePricingService { get; set; }

    // the rate being edited, null when no form is open
    // type RatePricingRequest.Edit with a ?
    private RatePricingRequest.Edit? editModel;

    // msg shown after a save that worked, string with a ?
    private string? successMessage;

    // msg shown when a save failed, string with a ?
    private string? saveErrorMessage;

    /// <summary>
    /// called once when the page opens, loads the rates from the api
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        // call GetIndexAsync of the service, await it, keep it in a var result
        var result = await RatePricingService.GetIndexAsync();

        // not a success, set the error msg, eg The rates could not be loaded, then return
        if (!result.IsSuccess)
        {
            errorMessage = "The rates could not be loaded.";
            rates = [];
            return;
        }

        // success, put result.Value.Rates in the list
        rates = result.Value.Rates;
    }

    /// <summary>
    /// opens the form for one rate, prefilled with its current price
    /// </summary>
    private void StartEdit(RatePricingDto.Index rate)
    {
        // hide the msgs of a previous save, both back to null
        saveErrorMessage = null;
        successMessage = null;

        // new edit model with the RateId and the PricePerNight of the rate
        editModel = new RatePricingRequest.Edit
        {
            RateId = rate.RateId,
            PricePerNight = rate.PricePerNight
        };
    }

    /// <summary>
    /// closes the form without saving
    /// </summary>
    private void CancelEdit()
    {
        // no rate edited anymore, back to null
        editModel = null;
    }

    /// <summary>
    /// called by the form only when the price is valid, sends it to the api
    /// </summary>
    private async Task SaveAsync()
    {
        // send the edit model with EditAsync of the service, await it, keep it in a var result
        var result = await RatePricingService.EditAsync(editModel!);

        // not a success, set the save error msg, then return
        if (!result.IsSuccess)
        {
            saveErrorMessage = "The price could not be saved. Please try again.";
            return;
        }

        // success, set the success msg, close the form, then reload the rates
        // reload: await OnInitializedAsync(), it loads the list again so the table shows the new price
        successMessage = "The price was saved.";
        editModel = null;
        await OnInitializedAsync();
    }

    /// <summary>
    /// price in eur with 2 decimals, eg 175 gives €175.00
    /// </summary>
    private static string FormatPrice(decimal price)
        => $"€{price.ToString("0.00", CultureInfo.InvariantCulture)}";
}
