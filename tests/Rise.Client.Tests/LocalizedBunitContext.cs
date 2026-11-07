using Rise.Client.Common;
using Rise.Client.I18nText;
using System.Reflection;
using System.Text.Json;

namespace Rise.Client;

public abstract class BunitContext : global::Bunit.BunitContext
{
    protected BunitContext()
    {
        Services.AddSingleton<IAppTextProvider, TestAppTextProvider>();
    }
}

internal sealed class TestAppTextProvider : IAppTextProvider
{
    public event EventHandler? LanguageChanged;

    /// <summary>
    /// lets a test act as if the guest picked an other language
    /// </summary>
    public void RaiseLanguageChanged() => LanguageChanged?.Invoke(this, EventArgs.Empty);

    public Task<AppText> GetTextAsync(Microsoft.AspNetCore.Components.ComponentBase component)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/Rise.Client/i18ntext/AppText.en.json"));
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var text = new AppText();

        foreach (var property in document.RootElement.EnumerateObject())
        {
            typeof(AppText)
                .GetField(property.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
                .SetValue(text, property.Value.GetString());
        }

        return Task.FromResult(text);
    }
}
