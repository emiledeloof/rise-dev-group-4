using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Rise.Client.Identity;

public partial class RedirectToLogin
{
    [CascadingParameter] private Task<AuthenticationState>? AuthenticationState { get; set; }
    [Inject] public required NavigationManager Navigation { get; set; }

    private bool _isAuthenticated;

    protected override async Task OnInitializedAsync()
    {
        var state = AuthenticationState is null ? null : await AuthenticationState;
        _isAuthenticated = state?.User.Identity?.IsAuthenticated ?? false;

        // Logged-in users without the required role stay here instead of looping back to the login.
        if (!_isAuthenticated)
        {
            var returnUrl = Uri.EscapeDataString(Navigation.ToBaseRelativePath(Navigation.Uri));
            Navigation.NavigateTo($"/login?returnUrl={returnUrl}", replace: true);
        }
    }
}
