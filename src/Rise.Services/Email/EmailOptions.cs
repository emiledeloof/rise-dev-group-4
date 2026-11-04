using System.ComponentModel.DataAnnotations;

namespace Rise.Services.Email;

/// <summary>
/// Strongly-typed SMTP settings bound from the <c>Email</c> configuration section.
/// </summary>
public class EmailOptions
{
    public const string SectionName = "Email";

    [Required]
    public required string SmtpHost { get; init; }

    public int SmtpPort { get; init; } = 587;

    public bool UseSsl { get; init; } = true;

    [Required]
    public required string FromAddress { get; init; }

    public string FromName { get; init; } = "Rise Hotel";

    public string? Username { get; init; }

    public string? Password { get; init; }
}
