using Microsoft.Extensions.DependencyInjection;
using Rise.Persistence;

using Rise.Services.Bookings;
using Rise.Services.Invoice;
using Rise.Services.Payments;
using Rise.Services.Products;
using Rise.Services.Projects;
using Rise.Shared.Bookings;
using Rise.Shared.Invoice;
using Rise.Shared.Products;
using Rise.Shared.Projects;

namespace Rise.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IProductService, ProductService>();        
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IStripePaymentGateway, StripePaymentGateway>();
        services.AddScoped<IStripeWebhookService, StripeWebhookService>();
        services.AddTransient<DbSeeder>();       
        
        // Add other application services here.
        return services;
    }
}