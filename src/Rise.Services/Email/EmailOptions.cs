
using System.ComponentModel.DataAnnotations;

namespace Rise.Services.Email;

/// <summary>
/// Strongly-typed SMTP settings bound from the <c>Email</c> configuration section.
/// When <see cref="SmtpHost"/> is absent or empty, <see cref="ServiceCollectionExtensions"/>
/// registers a <see cref="NullEmailService"/> so the application starts without SMTP config
/// (useful in CI and local development without Mailpit).
/// </summary>
public class EmailOptions : IValidatableObject
{
    public const string SectionName = "Email";

    /// <summary>
    /// SMTP server hostname. Leave empty to disable email sending (a no-op service is used instead).
    /// </summary>
    public string? SmtpHost { get; init; }

    [Range(1, 65535)]
    public int SmtpPort { get; init; } = 587;

    /// <summary>
    /// <c>true</c> → SSL on connect (port 465); <c>false</c> → STARTTLS when available (port 587).
    /// </summary>
    public bool UseSsl { get; init; } = false;

    [Required]
    [EmailAddress]
    public string FromAddress { get; init; } = "noreply@rise-hotel.example.com";

    [Required]
    public string FromName { get; init; } = "Rise Hotel";

    public string? Username { get; init; }

    public string? Password { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // If SmtpHost is empty/not configured, email is disabled -> everything is valid                                         
        if (string.IsNullOrWhiteSpace(SmtpHost))
            yield break;

        if (SmtpPort is < 1 or > 65535)
            yield return new ValidationResult("SmtpPort must be between 1 and 65535.", [nameof(SmtpPort)]);

        if (string.IsNullOrWhiteSpace(FromAddress))
            yield return new ValidationResult("FromAddress is required when SmtpHost is configured.", [nameof(FromAddress)]);
        else if (!new EmailAddressAttribute().IsValid(FromAddress))
            yield return new ValidationResult("FromAddress is not a valid email address.", [nameof(FromAddress)]);
    }
}
