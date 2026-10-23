using Microsoft.AspNetCore.Components;
using Rise.Shared.Identity.Accounts;

namespace Rise.Client.Identity;

public partial class Login
{
    [SupplyParameterFromQuery] private string? ReturnUrl { get; set; }

    private AccountRequest.Login Model = new();
    private Result _result = new();
    [Inject] public required IAccountManager AccountManager { get; set; }
    [Inject] public required NavigationManager Navigation { get; set; }
    public async Task LoginUser()
    {
        _result = await AccountManager.LoginAsync(Model.Email!, Model.Password!);

        if (_result.IsSuccess)
        {
            Navigation.NavigateTo(IsLocalUrl(ReturnUrl) ? $"/{ReturnUrl!.TrimStart('/')}" : "/admin");
        }
    }

    // Prevents open redirects: only relative paths within this app are accepted.
    private static bool IsLocalUrl(string? url) =>
        !string.IsNullOrWhiteSpace(url)
        && !url.StartsWith("//")
        && !url.StartsWith("/\\")
        && !url.Contains("://")
        && !url.Equals("login", StringComparison.OrdinalIgnoreCase);
}