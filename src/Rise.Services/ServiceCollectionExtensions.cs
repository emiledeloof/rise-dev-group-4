using Microsoft.Extensions.DependencyInjection;
using Rise.Persistence;

using Rise.Services.Bookings;
using Rise.Services.Invoice;
using Rise.Services.Payments;
using Rise.Shared.Bookings;
using Rise.Shared.Invoice;
using Rise.Services.Rooms;
using Rise.Shared.Rooms;

namespace Rise.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IStripePaymentGateway, StripePaymentGateway>();
        services.AddScoped<IStripeWebhookService, StripeWebhookService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddTransient<DbSeeder>();       
        
        // Add other application services here.
        return services;
    }
}