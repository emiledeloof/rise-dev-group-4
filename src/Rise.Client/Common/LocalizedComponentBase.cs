using Microsoft.AspNetCore.Components;
using Rise.Client.I18nText;

namespace Rise.Client.Common;

public interface IAppTextProvider
{
    Task<AppText> GetTextAsync(ComponentBase component);

    /// <summary>
    /// raised after the guest picked an other language, pages with texts from the api load them again
    /// </summary>
    event EventHandler? LanguageChanged;
}

public sealed class AppTextProvider : IAppTextProvider
{
    private readonly Toolbelt.Blazor.I18nText.I18nText _i18nText;

    public AppTextProvider(Toolbelt.Blazor.I18nText.I18nText i18nText)
    {
        _i18nText = i18nText;
        // * same lifetime as I18nText, so the subscription never outlives it
        _i18nText.ChangeLanguage += (_, _) => LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? LanguageChanged;

    public Task<AppText> GetTextAsync(ComponentBase component) =>
        _i18nText.GetTextTableAsync<AppText>(component);
}

public abstract class LocalizedComponentBase : ComponentBase
{
    [Inject] protected IAppTextProvider TextProvider { get; set; } = null!;

    protected AppText Text { get; set; } = new();

    protected Task LoadTextAsync() =>
        LoadTextAsyncCore();

    private async Task LoadTextAsyncCore()
    {
        Text = await TextProvider.GetTextAsync(this);
    }
}
