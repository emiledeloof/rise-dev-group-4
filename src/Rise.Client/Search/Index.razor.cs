using Rise.Shared.Search;

namespace Rise.Client.Search;

/// <summary>
/// stay search page, first step of a booking
/// </summary>
public partial class Index
{
    // dates picked in the calendar, reused later to search the available rooms
    private readonly SearchRequest.StayPeriod _stay = new();
}
