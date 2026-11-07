namespace Rise.Shared.Common;

/// <summary>
/// languages of the site, same list as the AppText files of the client and Rise.Domain.Common.Language
/// </summary>
public static class Languages
{
    /// <summary>
    /// language whose texts are required, shown when a translation is missing
    /// </summary>
    public const string Default = "en";

    /// <summary>
    /// two letter codes, the default language first
    /// </summary>
    public static readonly IReadOnlyList<string> Supported = ["en", "nl", "fr", "de", "it", "pl"];

    /// <summary>
    /// name of each language in English, for the labels of the admin forms
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> Names = new Dictionary<string, string>
    {
        ["en"] = "English",
        ["nl"] = "Dutch",
        ["fr"] = "French",
        ["de"] = "German",
        ["it"] = "Italian",
        ["pl"] = "Polish",
    };

    /// <summary>
    /// true for a code of <see cref="Supported"/>
    /// </summary>
    public static bool IsSupported(string? language) => language is not null && Supported.Contains(language);
}
