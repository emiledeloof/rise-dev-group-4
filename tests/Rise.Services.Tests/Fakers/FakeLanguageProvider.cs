using Rise.Services.Common;

namespace Rise.Services.Tests.Fakers;

/// <summary>
/// language of the guest for the tests, English unless a test asks for an other one
/// </summary>
public class FakeLanguageProvider(string language = "en") : ILanguageProvider
{
    public string Language { get; } = language;
}
