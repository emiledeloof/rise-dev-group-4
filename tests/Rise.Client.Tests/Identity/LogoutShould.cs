using Shouldly;

namespace Rise.Client.Identity;

public class LogoutShould : BunitContext
{
    private readonly FakeAccountManager _accountManager = new();

    public LogoutShould()
    {
        Services.AddSingleton<IAccountManager>(_accountManager);
    }

    [Fact]
    public void LogOutALoggedInAdmin()
    {
        this.AddAuthorization().SetAuthorized("TEST ADMIN");
        _accountManager.IsAuthenticated = true;

        Render<Logout>();

        _accountManager.LogoutCalls.ShouldBe(1);
    }

    [Fact]
    public void DoNothingWhenAlreadyLoggedOut()
    {
        this.AddAuthorization();

        var cut = Render<Logout>();

        _accountManager.LogoutCalls.ShouldBe(0);
        cut.Markup.ShouldContain("You're logged out.");
    }

    // only counts the logout calls, the login is not used by the page
    private sealed class FakeAccountManager : IAccountManager
    {
        public bool IsAuthenticated { get; set; }

        public int LogoutCalls { get; private set; }

        public Task<Ardalis.Result.Result> LoginAsync(string email, string password)
            => Task.FromResult(Ardalis.Result.Result.Success());

        public Task LogoutAsync()
        {
            LogoutCalls++;
            return Task.CompletedTask;
        }

        public Task<bool> CheckAuthenticatedAsync() => Task.FromResult(IsAuthenticated);
    }
}
