using Rise.Shared.ExtraServices;
using Rise.Shared.Identity;

namespace Rise.Server.Endpoints.ExtraServices;

/// <summary>
/// Endpoint to create a new extra service.
/// </summary>
public class Create(IExtraServiceService extraServiceService) : Endpoint<ExtraServiceRequest.Create, Result<ExtraServiceResponse.Create>>
{
    public override void Configure()
    {
        Post("/api/extra-services");
        Roles(AppRoles.Administrator);
    }

    public override Task<Result<ExtraServiceResponse.Create>> ExecuteAsync(ExtraServiceRequest.Create req, CancellationToken ct)
        => extraServiceService.CreateAsync(req, ct);
}
