using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Rise.Client;
using Rise.Client.Billing;
using Rise.Client.Common;
using Rise.Client.Bookings;
using Rise.Client.Guests;
using Rise.Client.Identity;
using Rise.Client.Invoice;
using Rise.Shared.Invoice;
using Rise.Client.Rooms;
using Rise.Shared.Billing;
using Rise.Shared.Bookings;
using Rise.Shared.Guests;
using Rise.Shared.Rooms;
using Rise.Client.Rates;
using Rise.Shared.Rates;
using Rise.Shared.RatePricing;
using Rise.Client.RatePricing;
using Toolbelt.Blazor.I18nText;
using Toolbelt.Blazor.Extensions.DependencyInjection;
using Rise.Client.RoomTypes;
using Rise.Shared.RoomTypes;

try
{
    var builder = WebAssemblyHostBuilder.CreateDefault(args);

    builder.RootComponents.Add<App>("#app");
    builder.RootComponents.Add<HeadOutlet>("head::after");


    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.BrowserConsole(outputTemplate: "[{Timestamp:HH:mm:ss}{Level:u3}]{Message:lj} {NewLine}{Exception}")
        .CreateLogger();

    Log.Information("Starting web application");

    // register the cookie handler
    builder.Services.AddTransient<CookieHandler>();

    // register i18n localization
    builder.Services.AddI18nText(options =>
    {
        options.PersistenceLevel = PersistanceLevel.SessionAndLocal;
    });
    builder.Services.AddScoped<IAppTextProvider, AppTextProvider>();

    // set up authorization
    builder.Services.AddAuthorizationCore();

    // register the custom state provider
    builder.Services.AddScoped<AuthenticationStateProvider, CookieAuthenticationStateProvider>();
    // register the account management interface
    builder.Services.AddScoped(sp => (IAccountManager)sp.GetRequiredService<AuthenticationStateProvider>());

    // the API is served from the same origin as the client (also in Docker), "BackendUrl" in wwwroot/appsettings.json can override it
    var backendUrl = new Uri(builder.Configuration["BackendUrl"] ?? builder.HostEnvironment.BaseAddress);

    // configure client for auth interactions
    builder.Services.AddHttpClient("SecureApi", opt => opt.BaseAddress = backendUrl)
        .AddHttpMessageHandler<CookieHandler>();

    // the room search is public, no auth cookie needed
    builder.Services.AddHttpClient<IRoomService, RoomService>(client =>
    {
        client.BaseAddress = backendUrl;
    });

    // booking creation, checkout, confirmation are public; the admin list needs the auth cookie — CookieHandler is ignored on anonymous endpoints
    builder.Services.AddHttpClient<IBookingService, BookingService>(client =>
    {
        client.BaseAddress = backendUrl;
    }).AddHttpMessageHandler<CookieHandler>();

    // the final bill is admin only, so send the auth cookie along
    builder.Services.AddHttpClient<IBillingService, BillingService>(client =>
    {
        client.BaseAddress = backendUrl;
    }).AddHttpMessageHandler<CookieHandler>();

    // the booking the guest is putting together, kept until "Pay now"
    builder.Services.AddScoped<BookingDraft>();

    builder.Services.AddHttpClient<IGuestService, GuestService>(client =>
    {
        client.BaseAddress = backendUrl;
    });

    // the invoice endpoint needs the admin role, so send the auth cookie along
    builder.Services.AddHttpClient<IInvoiceService, InvoiceService>(client =>
    {
        client.BaseAddress = backendUrl;
    }).AddHttpMessageHandler<CookieHandler>();

    builder.Services.AddHttpClient<IRateService, RateService>(client =>
    {
        client.BaseAddress = backendUrl;
    }).AddHttpMessageHandler<CookieHandler>();

    // the rate pricing endpoints need the admin role, so send the auth cookie along
    builder.Services.AddHttpClient<IRatePricingService, RatePricingService>(client =>
    {
        client.BaseAddress = backendUrl;
    }).AddHttpMessageHandler<CookieHandler>();

    // the room type endpoints need the admin role, so send the auth cookie along
    builder.Services.AddHttpClient<IRoomTypeService, RoomTypeService>(client =>
    {
        client.BaseAddress = backendUrl;
    }).AddHttpMessageHandler<CookieHandler>();

    // the admin room endpoints need the admin role, so send the auth cookie along
    builder.Services.AddHttpClient<IRoomAdminService, RoomAdminService>(client =>
    {
        client.BaseAddress = backendUrl;
    }).AddHttpMessageHandler<CookieHandler>();

    await builder.Build().RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "An exception occurred while creating the WASM host");
    throw;
}
