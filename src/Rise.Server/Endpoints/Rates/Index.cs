using Rise.Shared.Common;
using Rise.Shared.Identity;
using Rise.Shared.Rates;

namespace Rise.Server.Endpoints.Rates;

public class Index(IRateService rateService)
    : Endpoint<RateRequest.Index, Result<RateResponse.Index>>
{
    public override void Configure()
    {
        Get("/api/rates");
        Roles(AppRoles.Administrator);
    }
    public override Task<Result<RateResponse.Index>> ExecuteAsync(RateRequest.Index req, CancellationToken ct)
        => rateService.GetIndexAsync(req, ct);
}