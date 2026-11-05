using Rise.Shared.ExtraServices;
using Rise.Shared.Identity;

namespace Rise.Server.Endpoints.ExtraServices;

/// <summary>
/// Endpoint to update an existing extra service.
/// </summary>
public class Edit(IExtraServiceService extraServiceService) : Endpoint<ExtraServiceRequest.Edit, Result>
{
    public override void Configure()
    {
        Put("/api/extra-services");
        Roles(AppRoles.Administrator);
    }

    public override Task<Result> ExecuteAsync(ExtraServiceRequest.Edit req, CancellationToken ct)
        => extraServiceService.EditAsync(req, ct);
}
