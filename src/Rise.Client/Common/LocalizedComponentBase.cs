using Microsoft.AspNetCore.Components;
using Rise.Client.I18nText;

namespace Rise.Client.Common;

public interface IAppTextProvider
{
    Task<AppText> GetTextAsync(ComponentBase component);
}

public sealed class AppTextProvider(Toolbelt.Blazor.I18nText.I18nText i18nText) : IAppTextProvider
{
    public Task<AppText> GetTextAsync(ComponentBase component) =>
        i18nText.GetTextTableAsync<AppText>(component);
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
