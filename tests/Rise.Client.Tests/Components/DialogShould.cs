using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Components;

/// <summary>
/// tests for the shared <see cref="Dialog"/> component
/// </summary>
public class DialogShould : BunitContext
{
    private int _closeRequests;

    public DialogShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
        // the dialog takes the focus when it opens
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<Dialog> RenderDialog(Action<ComponentParameterCollectionBuilder<Dialog>>? configure = null, bool isOpen = true)
        => Render<Dialog>(parameters =>
        {
            parameters
                .Add(p => p.IsOpen, isOpen)
                .Add(p => p.Title, "Edit amenity")
                .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => _closeRequests++))
                .AddChildContent("<p class=\"body-text\">Body</p>");
            configure?.Invoke(parameters);
        });

    [Fact]
    public void RenderNothingWhenClosed()
    {
        var cut = RenderDialog(isOpen: false);

        cut.Markup.Trim().ShouldBeEmpty();
    }

    [Fact]
    public void ShowTheTitleAsTheAccessibleNameAndTheBody()
    {
        var cut = RenderDialog();

        var dialog = cut.Find("[role=dialog]");
        dialog.GetAttribute("aria-modal").ShouldBe("true");
        cut.Find($"#{dialog.GetAttribute("aria-labelledby")}").TextContent.ShouldBe("Edit amenity");
        cut.Find(".dialog-body .body-text").TextContent.ShouldBe("Body");
    }

    [Fact]
    public void LinkTheDescription()
    {
        var cut = RenderDialog(p => p.Add(d => d.Description, "Names and icon"));

        var describedBy = cut.Find("[role=dialog]").GetAttribute("aria-describedby");
        cut.Find($"#{describedBy}").TextContent.ShouldBe("Names and icon");
    }

    [Fact]
    public void RenderTheFooterOnlyWhenGiven()
    {
        RenderDialog().FindAll(".dialog-footer").ShouldBeEmpty();

        var cut = RenderDialog(p => p.Add(d => d.Footer, "<button class=\"save\">Save</button>"));

        cut.Find(".dialog-footer .save").ShouldNotBeNull();
    }

    [Fact]
    public void AskToCloseWithTheCross()
    {
        var cut = RenderDialog();

        cut.Find(".dialog-close").Click();

        _closeRequests.ShouldBe(1);
    }

    [Fact]
    public void AskToCloseWithEscape()
    {
        var cut = RenderDialog();

        cut.Find(".dialog").KeyDown("Escape");
        cut.Find(".dialog").KeyDown("Enter");

        _closeRequests.ShouldBe(1);
    }

    [Fact]
    public void AskToCloseWithAClickNextToIt()
    {
        var cut = RenderDialog();

        cut.Find(".dialog-backdrop").Click();

        _closeRequests.ShouldBe(1);
    }

    [Fact]
    public void StayOpenOnAClickNextToItWhenTurnedOff()
    {
        var cut = RenderDialog(p => p.Add(d => d.CloseOnBackdropClick, false));

        cut.Find(".dialog-backdrop").Click();

        _closeRequests.ShouldBe(0);
    }

    [Fact]
    public void BeAnAlertDialogForAQuestion()
    {
        var cut = RenderDialog(p => p.Add(d => d.IsAlert, true).Add(d => d.ShowCloseButton, false));

        cut.Find("[role=alertdialog]").ShouldNotBeNull();
        cut.FindAll(".dialog-close").ShouldBeEmpty();
    }

    [Fact]
    public void UseTheSizeAndClass()
    {
        var cut = RenderDialog(p => p.Add(d => d.Size, DialogSize.Large).Add(d => d.Class, "amenity-dialog"));

        cut.Find(".dialog-layer").ClassList.ShouldContain("amenity-dialog");
        cut.Find(".dialog").ClassList.ShouldContain("dialog-large");
    }
}
