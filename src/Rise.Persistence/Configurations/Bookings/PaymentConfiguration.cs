using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rise.Domain.Bookings;

namespace Rise.Persistence.Configurations.Bookings;

/// <summary>
/// Specific configuration for <see cref="Payment"/>.
/// </summary>
internal class PaymentConfiguration : EntityConfiguration<Payment>
{
    public override void Configure(EntityTypeBuilder<Payment> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.Amount).IsRequired();
        builder.Property(x => x.StripeCheckoutSession).HasMaxLength(250);
        // Unique, but nullable: multiple payments without a checkout session are allowed.
        builder.HasIndex(x => x.StripeCheckoutSession).IsUnique();
        builder.Property(x => x.StripeInvoice).HasMaxLength(250);
        builder.Property(x => x.InvoiceNumber).HasMaxLength(50);
        builder.Ignore(x => x.IsPaid);
    }
}
