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
        // stored as text so the rows stay readable, the migration marks the existing rows as Online (Stripe)
        builder.Property(x => x.Method)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(x => x.Reference).HasMaxLength(Payment.ReferenceMaxLength);
        builder.Property(x => x.StripeCheckoutSession).HasMaxLength(250);
        // Unique, but nullable: multiple payments without a checkout session are allowed.
        builder.HasIndex(x => x.StripeCheckoutSession).IsUnique();
        builder.Property(x => x.StripeInvoice).HasMaxLength(250);
        builder.Property(x => x.InvoiceNumber).HasMaxLength(50);
        // sort on the invoice list, low gain
        builder.HasIndex(x => x.InvoiceNumber);
        // paid or not filter on the booking list and the matrix, low gain
        builder.HasIndex(x => x.PaidAt);
        builder.Property(x => x.StripePaymentIntent).HasMaxLength(250);
        builder.Property(x => x.StripeReceiptUrl).HasMaxLength(500);
        builder.Property(x => x.CardBrand).HasMaxLength(50);
        builder.Property(x => x.CardLast4).HasMaxLength(4);
        builder.Ignore(x => x.IsPaid);
        builder.Ignore(x => x.IsOnSite);
    }
}
