namespace Rise.Shared.ExtraServices;

/// <summary>
/// Service contract for managing extra services.
/// Implemented by ExtraServiceService on the server and ExtraServiceClient on the client.
/// </summary>
public interface IExtraServiceService
{
    /// <summary>
    /// Gets all extra services, optionally filtered by active status only.
    /// </summary>
    Task<Result<ExtraServiceResponse.Index>> GetIndexAsync(bool onlyActive = false, CancellationToken ctx = default);

    /// <summary>
    /// Gets a single extra service by its ID.
    /// </summary>
    Task<Result<ExtraServiceDto.Index>> GetByIdAsync(int id, CancellationToken ctx = default);

    /// <summary>
    /// Creates a new extra service. Returns conflict if a service with the same name already exists.
    /// </summary>
    Task<Result<ExtraServiceResponse.Create>> CreateAsync(ExtraServiceRequest.Create request, CancellationToken ctx = default);

    /// <summary>
    /// Edits an existing extra service.
    /// </summary>
    Task<Result> EditAsync(ExtraServiceRequest.Edit request, CancellationToken ctx = default);

    /// <summary>
    /// Toggles the active status of an extra service.
    /// </summary>
    Task<Result> ToggleActiveAsync(int id, CancellationToken ctx = default);
}
