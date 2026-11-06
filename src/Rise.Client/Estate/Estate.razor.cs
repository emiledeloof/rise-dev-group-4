using Rise.Client.Common;

namespace Rise.Client.Estate;

public partial class Estate : LocalizedComponentBase
{
    // TODO: Load published estate content from the estate API when available, and show an empty state for missing content.
    private static readonly string[] EstatePhotos =
    [
        "images/home/estate.jpg",
        "images/home/estate-2.jpg",
        "images/home/estate-3.jpg",
        "images/home/estate-4.jpg",
        "images/home/estate-5.jpg",
    ];

    // title and description are keys of AppText, resolved in the razor with Text[...]
    private record EstateItem(string Icon, string TitleKey, string DescriptionKey);

    private static readonly EstateItem[] Highlights =
    [
        new("fa-seedling", "EstateVineyardsTitle", "EstateVineyardsText"),
        new("fa-leaf", "EstateGardensTitle", "EstateGardensText"),
        new("fa-mountain", "EstateWoodlandTitle", "EstateWoodlandText"),
        new("fa-bed", "EstateComfortTitle", "EstateComfortText"),
    ];

    private static readonly EstateItem[] Places =
    [
        new("fa-landmark", "EstateOldTownTitle", "EstateOldTownText"),
        new("fa-water", "EstateFonteNuovaTitle", "EstateFonteNuovaText"),
        new("fa-landmark-dome", "EstateHistoryTitle", "EstateHistoryText"),
        new("fa-train", "EstateTreninoTitle", "EstateTreninoText"),
    ];

    protected override Task OnInitializedAsync() => LoadTextAsync();
}
