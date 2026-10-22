using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Rise.Client.Components;

/// <summary>
/// shows one photo at a time with previous and next arrows, eg the photos of a room on the home
/// </summary>
public partial class PhotoCarousel
{
    /// <summary>
    /// paths of the photos, in order, eg images/rooms/standard-room-1.jpg
    /// </summary>
    [Parameter, EditorRequired] public IReadOnlyList<string> Photos { get; set; } = [];

    /// <summary>
    /// short description of what the photos show, eg the room name
    /// </summary>
    [Parameter] public string Alt { get; set; } = string.Empty;

    /// <summary>
    /// when true a click on the photo opens it large, with a switch to its original size, eg on the room detail
    /// </summary>
    [Parameter] public bool Zoomable { get; set; }

    // position of the photo on screen in Photos, starts on the 1st one
    private int _index;

    // true while the photo is shown large
    private bool _isOpen;

    // true when the large photo has its own pixel size instead of fitting the screen
    private bool _isOriginalSize;

    // true right after opening, so the dialog gets the focus once and the keys work at once
    private bool _focusLightbox;

    private ElementReference _lightbox;

    // alt text with the position, eg Deluxe Room, photo 2 of 5
    private string PhotoAlt => $"{Alt}, photo {_index + 1} of {Photos.Count}";

    // next photo, back to the 1st one after the last one, a zoomed photo goes back to fitted
    private void Next()
    {
        _index = (_index + 1) % Photos.Count;
        _isOriginalSize = false;
    }

    // previous photo, goes to the last one before the 1st one, a zoomed photo goes back to fitted
    private void Previous()
    {
        _index = (_index - 1 + Photos.Count) % Photos.Count;
        _isOriginalSize = false;
    }

    // shows the current photo large
    private void Open()
    {
        if (!Zoomable)
        {
            return;
        }

        _isOpen = true;
        _focusLightbox = true;
    }

    // back to the slider, the next open starts fitted to the screen again
    private void Close()
    {
        _isOpen = false;
        _isOriginalSize = false;
    }

    private void ToggleOriginalSize() => _isOriginalSize = !_isOriginalSize;

    // escape closes, the arrow keys switch the photo
    private void OnKeyDown(KeyboardEventArgs e)
    {
        switch (e.Key)
        {
            case "Escape":
                Close();
                break;
            case "ArrowRight" when Photos.Count > 1:
                Next();
                break;
            case "ArrowLeft" when Photos.Count > 1:
                Previous();
                break;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusLightbox)
        {
            _focusLightbox = false;
            await _lightbox.FocusAsync();
        }
    }
}
