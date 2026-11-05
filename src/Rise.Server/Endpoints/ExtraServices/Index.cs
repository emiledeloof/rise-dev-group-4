using Rise.Shared.ExtraServices;
using Rise.Shared.Identity;

namespace Rise.Server.Endpoints.ExtraServices;

/// <summary>
/// Endpoint to retrieve all extra services.
/// </summary>
public class Index(IExtraServiceService extraServiceService) : EndpointWithoutRequest<Result<ExtraServiceResponse.Index>>
{
    public override void Configure()
    {
        Get("/api/extra-services");
        Roles(AppRoles.Administrator);
    }

    public override Task<Result<ExtraServiceResponse.Index>> ExecuteAsync(CancellationToken ct)
    {
        var onlyActive = Query<bool>("onlyActive", isRequired: false);
        return extraServiceService.GetIndexAsync(onlyActive, ct);
    }
}
