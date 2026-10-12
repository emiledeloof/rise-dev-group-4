using Ardalis.Result;
using Rise.Shared.Guests;

namespace Rise.Client.Guests;

/// <summary>
/// In-memory fake implementation of <see cref="IGuestService"/> used for bUnit client tests.
/// Avoids external network calls and provides deterministic responses.
/// </summary>
public class FakeGuestService : IGuestService
{
    public Result<GuestResponse.Create> ResultToReturn { get; set; } = 
        Result.Created(new GuestResponse.Create { GuestId = 1 });

    public Task<Result<GuestResponse.Create>> CreateAsync(GuestRequest.Create request, CancellationToken ctx = default)
    {
        return Task.FromResult(ResultToReturn);
    }
}
