using Rise.Shared.ExtraServices;
using Rise.Shared.Identity;

namespace Rise.Server.Endpoints.ExtraServices;

/// <summary>
/// Endpoint to toggle the active state of an extra service.
/// </summary>
public class ToggleActive(IExtraServiceService extraServiceService) : EndpointWithoutRequest<Result>
{
    public override void Configure()
    {
        Post("/api/extra-services/{id:int}/toggle-active");
        Roles(AppRoles.Administrator);
    }

    public override Task<Result> ExecuteAsync(CancellationToken ct)
        => extraServiceService.ToggleActiveAsync(Route<int>("id"), ct);
}
