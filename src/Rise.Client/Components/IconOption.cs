namespace Rise.Client.Components;

/// <summary>
/// one choice of an IconSelect, the Font Awesome class and the text shown in the dropdown
/// </summary>
public sealed record IconOption(string Value, string Label)
{
    /// <summary>
    /// Font Awesome icons for the amenities and other room features, the default choices of an IconSelect
    /// </summary>
    public static IReadOnlyList<IconOption> Defaults { get; } =
    [
        new("fa-baby", "Baby"),
        new("fa-ban-smoking", "No smoking"),
        new("fa-bath", "Bathtub"),
        new("fa-bed", "Bed"),
        new("fa-check", "Check mark"),
        new("fa-couch", "Couch"),
        new("fa-mug-saucer", "Cup and saucer"),
        new("fa-dog", "Dog"),
        new("fa-temperature-low", "Cold"),
        new("fa-temperature-high", "Heat"),
        new("fa-mug-hot", "Hot drink"),
        new("fa-lock", "Lock"),
        new("fa-square-parking", "Parking"),
        new("fa-phone", "Phone"),
        new("fa-shirt", "Shirt"),
        new("fa-shower", "Shower"),
        new("fa-snowflake", "Snowflake"),
        new("fa-sun", "Sun"),
        new("fa-tree", "Tree"),
        new("fa-tv", "TV"),
        new("fa-water", "Water"),
        new("fa-wifi", "Wi-Fi"),
        new("fa-wind", "Wind"),
        new("fa-wine-glass", "Wine glass"),
    ];
}
