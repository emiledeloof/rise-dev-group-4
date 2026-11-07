namespace Rise.Domain.Common;

/// <summary>
/// languages the guest can pick on the site, same list as Rise.Shared.Common.Languages and the AppText files of the client
/// </summary>
public static class Language
{
    /// <summary>
    /// language of the texts that must always exist, used when a translation is missing
    /// </summary>
    public const string Default = "en";

    /// <summary>
    /// all languages of the site, two letter codes
    /// </summary>
    public static readonly IReadOnlyList<string> Supported = ["en", "nl", "fr", "de", "it", "pl"];

    /// <summary>
    /// the lower case code when it is supported, an exception otherwise
    /// </summary>
    public static string EnsureSupported(string language)
    {
        var code = Guard.Against.NullOrWhiteSpace(language).Trim().ToLowerInvariant();

        if (!Supported.Contains(code))
            throw new ArgumentException($"Language '{language}' is not supported.", nameof(language));

        return code;
    }
}
