using Rise.Services.Common;
using Rise.Shared.Common;

namespace Rise.Server.Identity;

/// <summary>
/// language of the guest from the Accept-Language header the client sends, English when none is supported
/// </summary>
// ? reads the header instead of UseRequestLocalization, so the culture of the request (number and date formats) does not change
public class AcceptLanguageProvider(IHttpContextAccessor httpContextAccessor) : ILanguageProvider
{
    /// <summary>
    /// the supported language with the highest quality in the header, eg nl for "nl-BE,nl;q=0.9,en;q=0.8"
    /// </summary>
    public string Language
    {
        get
        {
            var request = httpContextAccessor.HttpContext?.Request;
            if (request is null)
                return Languages.Default;

            var preferred = request.GetTypedHeaders().AcceptLanguage
                .OrderByDescending(value => value.Quality ?? 1)
                // nl-BE becomes nl, the site only knows the two letter codes
                .Select(value => value.Value.Value?.Split('-')[0].ToLowerInvariant())
                .FirstOrDefault(Languages.IsSupported);

            return preferred ?? Languages.Default;
        }
    }
}
