using FastEndpoints;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Rise.Services.Payments;
using Rise.Shared.Billing;
using Rise.Shared.Bookings;
using Rise.Shared.Guests;
using Rise.Shared.Identity;
using Rise.Shared.Invoice;
using Rise.Shared.RatePricing;
using Rise.Shared.Rates;
using Rise.Shared.Rooms;
using Rise.Shared.RoomTypes;
using BillingEndpoints = Rise.Server.Endpoints.Billing;
using BookingsEndpoints = Rise.Server.Endpoints.Bookings;
using GuestsEndpoints = Rise.Server.Endpoints.Guests;
using InvoicesEndpoints = Rise.Server.Endpoints.Invoice;
using PaymentsEndpoints = Rise.Server.Endpoints.Payments;
using RatePricingEndpoints = Rise.Server.Endpoints.RatePricing;
using RatesEndpoints = Rise.Server.Endpoints.Rates;
using RoomsEndpoints = Rise.Server.Endpoints.Rooms;
using RoomTypesEndpoints = Rise.Server.Endpoints.RoomTypes;

namespace Rise.Services.Tests.Security;

/// <summary>
/// checks the access rights written in the Configure method of every endpoint,
/// so removing Roles or adding AllowAnonymous by mistake makes a test fail
/// </summary>
public class EndpointAccessShould
{
    /// <summary>
    /// every admin screen talks to these endpoints, a guest must never reach them
    /// </summary>
    [Fact]
    public void LetOnlyAdministratorsUseTheAdminEndpoints()
    {
        var adminEndpoints = new Dictionary<string, EndpointDefinition>
        {
            ["bill detail"] = Configured<BillingEndpoints.Detail>(Substitute.For<IBillingService>()),
            ["bill add line"] = Configured<BillingEndpoints.AddLine>(Substitute.For<IBillingService>()),
            ["bill edit line"] = Configured<BillingEndpoints.EditLine>(Substitute.For<IBillingService>()),
            ["bill delete line"] = Configured<BillingEndpoints.DeleteLine>(Substitute.For<IBillingService>()),
            ["booking list"] = Configured<BookingsEndpoints.Index>(Substitute.For<IBookingService>()),
            ["booking detail"] = Configured<BookingsEndpoints.Detail>(Substitute.For<IBookingService>()),
            ["booking cancel"] = Configured<BookingsEndpoints.Cancel>(Substitute.For<IBookingService>()),
            ["booking matrix"] = Configured<BookingsEndpoints.Matrix>(Substitute.For<IBookingService>()),
            ["invoice list"] = Configured<InvoicesEndpoints.Index>(Substitute.For<IInvoiceService>()),
            ["rate pricing edit"] = Configured<RatePricingEndpoints.Edit>(Substitute.For<IRatePricingService>()),
            ["vat settings read"] = Configured<RatePricingEndpoints.GetSettings>(Substitute.For<IRatePricingService>()),
            ["vat settings update"] = Configured<RatePricingEndpoints.UpdateSettings>(Substitute.For<IRatePricingService>()),
            ["rate list"] = Configured<RatesEndpoints.Index>(Substitute.For<IRateService>()),
            ["rate create"] = Configured<RatesEndpoints.Create>(Substitute.For<IRateService>()),
            ["physical room list"] = Configured<RoomsEndpoints.AdminIndex>(Substitute.For<IRoomAdminService>()),
            ["physical room create"] = Configured<RoomsEndpoints.AdminCreate>(Substitute.For<IRoomAdminService>()),
            ["room type list"] = Configured<RoomTypesEndpoints.Index>(Substitute.For<IRoomTypeService>()),
            ["room type create"] = Configured<RoomTypesEndpoints.Create>(Substitute.For<IRoomTypeService>()),
            ["room type edit"] = Configured<RoomTypesEndpoints.Edit>(Substitute.For<IRoomTypeService>()),
            ["room type delete"] = Configured<RoomTypesEndpoints.Delete>(Substitute.For<IRoomTypeService>()),
        };

        foreach (var (name, definition) in adminEndpoints)
        {
            // * the endpoint name in the message tells which file to open when it fails
            definition.AllowedRoles.ShouldNotBeNull($"{name} should be admin only");
            definition.AllowedRoles.Distinct().ShouldBe(new[] { AppRoles.Administrator }, customMessage: $"{name} should be admin only");
            definition.AnonymousVerbs.ShouldBeNull($"{name} should not allow anonymous calls");
        }
    }

    /// <summary>
    /// a visitor searches, books and pays without an account, these endpoints must stay open
    /// </summary>
    [Fact]
    public void LetVisitorsUseTheBookingFlowWithoutAnAccount()
    {
        var publicEndpoints = new Dictionary<string, EndpointDefinition>
        {
            ["room list"] = Configured<RoomsEndpoints.List>(Substitute.For<IRoomService>()),
            ["room detail"] = Configured<RoomsEndpoints.Detail>(Substitute.For<IRoomService>()),
            ["room search"] = Configured<RoomsEndpoints.Available>(Substitute.For<IRoomService>()),
            ["guest create"] = Configured<GuestsEndpoints.Create>(Substitute.For<IGuestService>()),
            ["booking create"] = Configured<BookingsEndpoints.Create>(Substitute.For<IBookingService>()),
            ["checkout session create"] = Configured<BookingsEndpoints.CreateCheckoutSession>(Substitute.For<IBookingService>()),
            ["booking after payment"] = Configured<BookingsEndpoints.DetailByCheckoutSession>(Substitute.For<IBookingService>()),
        };

        foreach (var (name, definition) in publicEndpoints)
        {
            definition.AnonymousVerbs.ShouldNotBeNull($"{name} should allow anonymous calls");
            definition.AnonymousVerbs.ShouldNotBeEmpty($"{name} should allow anonymous calls");
        }
    }

    /// <summary>
    /// stripe calls the webhook without a cookie, it is protected by its signature instead of a role
    /// </summary>
    [Fact]
    public void LetStripeCallTheWebhookWithoutAnAccount()
    {
        var definition = Configured<PaymentsEndpoints.StripeWebhook>(
            Substitute.For<IConfiguration>(),
            Substitute.For<IStripeWebhookService>(),
            Substitute.For<ILogger<PaymentsEndpoints.StripeWebhook>>());

        definition.AnonymousVerbs.ShouldNotBeNull();
        definition.AllowedRoles.ShouldBeNull();
    }

    // creates the endpoint like fastendpoints does and runs its Configure method
    private static EndpointDefinition Configured<TEndpoint>(params object[] dependencies)
        where TEndpoint : class, IEndpoint
    {
        var endpoint = Factory.Create<TEndpoint>(dependencies);
        ((BaseEndpoint)(object)endpoint).Configure();
        return endpoint.Definition;
    }
}
