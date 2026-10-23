using Shouldly;
using Xunit.Abstractions;

namespace Rise.Client.Components;

/// <summary>
/// tests for the <see cref="PhotoCarousel"/> of the home room cards
/// each test follows 3 steps, arrange to prepare, act to do the action, assert to check the result
/// </summary>
public class PhotoCarouselShould : BunitContext
{
    /// <summary>
    /// sets up the xunit logger like the other client tests
    /// </summary>
    public PhotoCarouselShould(ITestOutputHelper outputHelper)
    {
        Services.AddXunitLogger(outputHelper);
        // the lightbox takes the focus when it opens
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    // renders the carousel with the given photos for the deluxe room
    private IRenderedComponent<PhotoCarousel> RenderCarousel(params string[] photos) =>
        Render<PhotoCarousel>(p => p
            .Add(x => x.Photos, photos)
            .Add(x => x.Alt, "Deluxe Room"));

    // renders the carousel of the room detail, where a click on the photo opens it large
    private IRenderedComponent<PhotoCarousel> RenderZoomableCarousel(params string[] photos) =>
        Render<PhotoCarousel>(p => p
            .Add(x => x.Photos, photos)
            .Add(x => x.Alt, "Deluxe Room")
            .Add(x => x.Zoomable, true));

    // src of the photo on screen
    private static string CurrentPhoto(IRenderedComponent<PhotoCarousel> cut) =>
        cut.Find(".carousel-photo").GetAttribute("src")!;

    /// <summary>
    /// the 1st photo is shown first, with its position in the alt text
    /// </summary>
    [Fact]
    public void ShowTheFirstPhotoFirst()
    {
        // arrange and act
        var cut = RenderCarousel("a.jpg", "b.jpg", "c.jpg");

        // assert
        CurrentPhoto(cut).ShouldBe("a.jpg");
        cut.Find(".carousel-photo").GetAttribute("alt").ShouldBe("Deluxe Room, photo 1 of 3");
    }

    /// <summary>
    /// the next arrow shows the next photo
    /// </summary>
    [Fact]
    public void ShowTheNextPhoto()
    {
        // arrange
        var cut = RenderCarousel("a.jpg", "b.jpg", "c.jpg");

        // act
        cut.Find(".carousel-next").Click();

        // assert
        CurrentPhoto(cut).ShouldBe("b.jpg");
    }

    /// <summary>
    /// next on the last photo goes back to the 1st one
    /// </summary>
    [Fact]
    public void GoBackToTheFirstPhotoAfterTheLastOne()
    {
        // arrange
        var cut = RenderCarousel("a.jpg", "b.jpg");

        // act
        cut.Find(".carousel-next").Click();
        cut.Find(".carousel-next").Click();

        // assert
        CurrentPhoto(cut).ShouldBe("a.jpg");
    }

    /// <summary>
    /// previous on the 1st photo goes to the last one
    /// </summary>
    [Fact]
    public void GoToTheLastPhotoBeforeTheFirstOne()
    {
        // arrange
        var cut = RenderCarousel("a.jpg", "b.jpg", "c.jpg");

        // act
        cut.Find(".carousel-prev").Click();

        // assert
        CurrentPhoto(cut).ShouldBe("c.jpg");
    }

    /// <summary>
    /// one dot per photo, the one of the photo on screen is active
    /// </summary>
    [Fact]
    public void ShowOneDotPerPhoto()
    {
        // arrange
        var cut = RenderCarousel("a.jpg", "b.jpg", "c.jpg");

        // act
        cut.Find(".carousel-next").Click();

        // assert
        var dots = cut.FindAll(".carousel-dot");
        dots.Count.ShouldBe(3);
        dots[1].ClassList.ShouldContain("is-active");
    }

    /// <summary>
    /// a single photo needs no arrows and no dots
    /// </summary>
    [Fact]
    public void HideTheArrowsWithOnePhoto()
    {
        // arrange and act
        var cut = RenderCarousel("a.jpg");

        // assert
        cut.FindAll(".carousel-btn").ShouldBeEmpty();
        cut.FindAll(".carousel-dot").ShouldBeEmpty();
    }

    /// <summary>
    /// no photos renders nothing instead of crashing
    /// </summary>
    [Fact]
    public void RenderNothingWithoutPhotos()
    {
        // arrange and act
        var cut = RenderCarousel();

        // assert
        cut.FindAll(".carousel").ShouldBeEmpty();
    }

    /// <summary>
    /// the home cards have no detail mode, the photo is not a btn
    /// </summary>
    [Fact]
    public void NotOpenTheLightboxWhenNotZoomable()
    {
        // arrange and act
        var cut = RenderCarousel("a.jpg", "b.jpg");

        // assert
        cut.FindAll(".carousel-zoom").ShouldBeEmpty();
        cut.FindAll(".lightbox").ShouldBeEmpty();
    }

    /// <summary>
    /// a click on the photo opens the photo on screen large, fitted to the screen
    /// </summary>
    [Fact]
    public void OpenTheLightboxOnTheCurrentPhoto()
    {
        // arrange
        var cut = RenderZoomableCarousel("a.jpg", "b.jpg", "c.jpg");
        cut.Find(".carousel-next").Click();

        // act
        cut.Find(".carousel-zoom").Click();

        // assert
        cut.Find(".lightbox-photo").GetAttribute("src").ShouldBe("b.jpg");
        cut.Find(".lightbox-counter").TextContent.ShouldBe("2 / 3");
        cut.Find(".lightbox-stage").ClassList.ShouldNotContain("is-original");
    }

    /// <summary>
    /// the size btn switches between the original size and fitted to the screen
    /// </summary>
    [Fact]
    public void SwitchToTheOriginalSize()
    {
        // arrange
        var cut = RenderZoomableCarousel("a.jpg", "b.jpg");
        cut.Find(".carousel-zoom").Click();

        // act
        cut.Find(".lightbox-size").Click();

        // assert
        cut.Find(".lightbox-stage").ClassList.ShouldContain("is-original");

        // act again
        cut.Find(".lightbox-size").Click();

        // assert again
        cut.Find(".lightbox-stage").ClassList.ShouldNotContain("is-original");
    }

    /// <summary>
    /// the arrows in the lightbox switch the photo, also in the slider behind it
    /// </summary>
    [Fact]
    public void ShowTheNextPhotoInTheLightbox()
    {
        // arrange
        var cut = RenderZoomableCarousel("a.jpg", "b.jpg", "c.jpg");
        cut.Find(".carousel-zoom").Click();

        // act
        cut.Find(".lightbox-next").Click();

        // assert
        cut.Find(".lightbox-photo").GetAttribute("src").ShouldBe("b.jpg");
        CurrentPhoto(cut).ShouldBe("b.jpg");
    }

    /// <summary>
    /// the arrow keys switch the photo too
    /// </summary>
    [Fact]
    public void SwitchThePhotoWithTheArrowKeys()
    {
        // arrange
        var cut = RenderZoomableCarousel("a.jpg", "b.jpg", "c.jpg");
        cut.Find(".carousel-zoom").Click();

        // act
        cut.Find(".lightbox").KeyDown(Key.Left);

        // assert
        cut.Find(".lightbox-photo").GetAttribute("src").ShouldBe("c.jpg");
    }

    /// <summary>
    /// a zoomed photo does not keep the zoom for the next photo, it is fitted to the screen again
    /// </summary>
    [Fact]
    public void FitTheNextPhotoToTheScreen()
    {
        // arrange
        var cut = RenderZoomableCarousel("a.jpg", "b.jpg", "c.jpg");
        cut.Find(".carousel-zoom").Click();
        cut.Find(".lightbox-size").Click();

        // act
        cut.Find(".lightbox-next").Click();

        // assert
        cut.Find(".lightbox-photo").GetAttribute("src").ShouldBe("b.jpg");
        cut.Find(".lightbox-stage").ClassList.ShouldNotContain("is-original");
    }

    /// <summary>
    /// switching the photo with an arrow key also ends the zoom
    /// </summary>
    [Fact]
    public void FitThePhotoToTheScreenAfterAnArrowKey()
    {
        // arrange
        var cut = RenderZoomableCarousel("a.jpg", "b.jpg", "c.jpg");
        cut.Find(".carousel-zoom").Click();
        cut.Find(".lightbox-size").Click();

        // act
        cut.Find(".lightbox").KeyDown(Key.Left);

        // assert
        cut.Find(".lightbox-photo").GetAttribute("src").ShouldBe("c.jpg");
        cut.Find(".lightbox-stage").ClassList.ShouldNotContain("is-original");
    }

    /// <summary>
    /// the close btn goes back to the slider
    /// </summary>
    [Fact]
    public void CloseTheLightboxWithTheCloseButton()
    {
        // arrange
        var cut = RenderZoomableCarousel("a.jpg", "b.jpg");
        cut.Find(".carousel-zoom").Click();

        // act
        cut.Find(".lightbox-close").Click();

        // assert
        cut.FindAll(".lightbox").ShouldBeEmpty();
    }

    /// <summary>
    /// escape closes the lightbox, the next open is fitted to the screen again
    /// </summary>
    [Fact]
    public void CloseTheLightboxWithEscape()
    {
        // arrange
        var cut = RenderZoomableCarousel("a.jpg", "b.jpg");
        cut.Find(".carousel-zoom").Click();
        cut.Find(".lightbox-size").Click();

        // act
        cut.Find(".lightbox").KeyDown(Key.Escape);

        // assert
        cut.FindAll(".lightbox").ShouldBeEmpty();
        cut.Find(".carousel-zoom").Click();
        cut.Find(".lightbox-stage").ClassList.ShouldNotContain("is-original");
    }
}
