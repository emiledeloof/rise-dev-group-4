using System.Security.Claims;

namespace Rise.Services.Identity;

/// <summary>
/// Interace for the session context provider, can be easily mocked in test case scenarios.
/// So if you're testing a Service that uses the session provider, you can mock the provider.
/// For example with the FakeSessionContextProvider in the Rise.Services.Tests project.
/// </summary>
public interface ISessionContextProvider
{
    ClaimsPrincipal? User { get; }
}