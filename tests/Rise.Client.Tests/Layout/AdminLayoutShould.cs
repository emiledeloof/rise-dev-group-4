using Microsoft.AspNetCore.Components;
using Shouldly;

namespace Rise.Client.Layout;

public class AdminLayoutShould : BunitContext
{
    private IRenderedComponent<AdminLayout> RenderLayout()
        => Render<AdminLayout>(p => p.Add(x => x.Body, (RenderFragment)(b => b.AddContent(0, "page"))));

    [Fact]
    public void LinkToTheLogoutPage()
    {
        var cut = RenderLayout();

        var logout = cut.Find(".admin-logout");
        logout.GetAttribute("href").ShouldBe("/logout");
        logout.TextContent.Trim().ShouldBe("Log out");
    }

    [Fact]
    public void KeepTheLinkBackToTheSite()
    {
        var cut = RenderLayout();

        cut.FindAll(".admin-back").Select(a => a.GetAttribute("href")).ShouldBe(["/", "/logout"]);
    }
}
