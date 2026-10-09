using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Rise.Client;
using Rise.Client.Identity;
using Rise.Client.Invoice;
using Rise.Shared.Invoice;
using Rise.Client.Rooms;
using Rise.Shared.Rooms;

try
{
    var builder = WebAssemblyHostBuilder.CreateDefault(args);

    builder.RootComponents.Add<App>("#app");
    builder.RootComponents.Add<HeadOutlet>("head::after");
    

    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.BrowserConsole(outputTemplate:"[{Timestamp:HH:mm:ss}{Level:u3}]{Message:lj} {NewLine}{Exception}")
        .CreateLogger();
    
    Log.Information("Starting web application");

// register the cookie handler
    builder.Services.AddTransient<CookieHandler>();

// set up authorization
    builder.Services.AddAuthorizationCore();

// register the custom state provider
    builder.Services.AddScoped<AuthenticationStateProvider, CookieAuthenticationStateProvider>();
// register the account management interface
    builder.Services.AddScoped(sp => (IAccountManager)sp.GetRequiredService<AuthenticationStateProvider>());

// the API is served from the same origin as the client (also in Docker), "BackendUrl" in wwwroot/appsettings.json can override it
    var backendUrl = new Uri(builder.Configuration["BackendUrl"] ?? builder.HostEnvironment.BaseAddress);

// configure client for auth interactions
    builder.Services.AddHttpClient("SecureApi",opt => opt.BaseAddress = backendUrl)
        .AddHttpMessageHandler<CookieHandler>();

    // the room search is public, no auth cookie needed
    builder.Services.AddHttpClient<IRoomService, RoomService>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["BackendUrl"] ?? "https://localhost:5001");
    });

    // the invoice endpoint needs the admin role, so send the auth cookie along
    builder.Services.AddHttpClient<IInvoiceService, InvoiceService>(client =>
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
