namespace Rise.Services.Common;

/// <summary>
/// language of the current request, so the services can send the texts of the guest
/// implemented in the server from the Accept-Language header, tests use a fake
/// </summary>
public interface ILanguageProvider
{
    /// <summary>
    /// two letter code of a supported language, the default language when the guest asked for none or an unknown one
    /// </summary>
    string Language { get; }
}
