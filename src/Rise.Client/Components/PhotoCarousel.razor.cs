using Microsoft.AspNetCore.Components;

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

    // position of the photo on screen in Photos, starts on the 1st one
    private int _index;

    // alt text with the position, eg Deluxe Room, photo 2 of 5
    private string PhotoAlt => $"{Alt}, photo {_index + 1} of {Photos.Count}";

    // next photo, back to the 1st one after the last one
    private void Next() => _index = (_index + 1) % Photos.Count;

    // previous photo, goes to the last one before the 1st one
    private void Previous() => _index = (_index - 1 + Photos.Count) % Photos.Count;
}
