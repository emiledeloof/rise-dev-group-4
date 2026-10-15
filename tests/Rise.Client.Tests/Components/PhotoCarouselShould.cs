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
    }

    // renders the carousel with the given photos for the deluxe room
    private IRenderedComponent<PhotoCarousel> RenderCarousel(params string[] photos) =>
        Render<PhotoCarousel>(p => p
            .Add(x => x.Photos, photos)
            .Add(x => x.Alt, "Deluxe Room"));

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
}
