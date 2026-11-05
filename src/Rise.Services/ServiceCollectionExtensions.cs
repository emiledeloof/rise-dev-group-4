using Microsoft.Extensions.DependencyInjection;
using Rise.Persistence;

using Rise.Services.Bookings;
using Rise.Services.ExtraServices;
using Rise.Services.Guests;
using Rise.Services.Invoice;
using Rise.Services.Payments;
using Rise.Services.Rates;
using Rise.Shared.Bookings;
using Rise.Shared.ExtraServices;
using Rise.Shared.Guests;
using Rise.Shared.Invoice;
using Rise.Services.Rooms;
using Rise.Shared.Rates;
using Rise.Shared.Rooms;
using Rise.Services.RatePricing;
using Rise.Shared.RatePricing;
using Rise.Services.RoomTypes;
using Rise.Shared.RoomTypes;

namespace Rise.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IGuestService, GuestService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IStripePaymentGateway, StripePaymentGateway>();
        services.AddScoped<IStripeWebhookService, StripeWebhookService>();
        services.AddScoped<IRateService, RateService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IRatePricingService, RatePricingService>();
        services.AddScoped<IRoomTypeService, RoomTypeService>();
        services.AddScoped<IExtraServiceService, ExtraServiceService>();
        services.AddTransient<DbSeeder>();

        // Add other application services here.
        return services;
    }
}