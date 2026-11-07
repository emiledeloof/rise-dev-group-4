using Rise.Domain.Common;

namespace Rise.Services.Common;

/// <summary>
/// picks the text of a language out of translation rows read from the database
/// </summary>
internal static class Translations
{
    /// <summary>
    /// the text in the asked language, the default language text when it is missing, null when there is none
    /// </summary>
    public static string? Pick<T>(IEnumerable<T> rows, Func<T, string> languageOf, Func<T, string> textOf, string language)
    {
        var list = rows as IReadOnlyCollection<T> ?? rows.ToList();
        var row = list.FirstOrDefault(r => languageOf(r) == language)
            ?? list.FirstOrDefault(r => languageOf(r) == Language.Default);
        return row is null ? null : textOf(row);
    }
}
