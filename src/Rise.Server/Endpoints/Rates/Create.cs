using Rise.Shared.Identity;
using Rise.Shared.Rates;

namespace Rise.Server.Endpoints.Rates;

public class Create(IRateService rateService)
    : Endpoint<RateRequest.Create, Result<RateResponse.Create>>
{
    public override void Configure()
    {
        Post("/api/rates");
        Roles(AppRoles.Administrator);
    }
    public override Task<Result<RateResponse.Create>> ExecuteAsync(RateRequest.Create req, CancellationToken ct)
        => rateService.CreateAsync(req, ct);
}