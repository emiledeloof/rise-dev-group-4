using Microsoft.AspNetCore.Components;
using Rise.Shared.Guests;

namespace Rise.Client.Guests;

/// <summary>
/// Code-behind class for the guest registration page (<see cref="Create"/>).
/// Encapsulates view logic, dependency injection, and user interaction.
/// </summary>
public partial class Create
{
    /// <summary>
    /// Service used to send guest registration data to the backend API.
    /// Injected via Dependency Injection (can be swapped with a test fake in bUnit tests).
    /// </summary>
    [Inject]
    public required IGuestService GuestService { get; set; }

    /// <summary>
    /// Navigation manager used to redirect the user upon successful registration.
    /// </summary>
    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    /// <summary>
    /// The form model bound to the EditForm. Validated by FluentValidation.
    /// </summary>
    protected GuestRequest.Create Model { get; set; } = new();

    /// <summary>
    /// Holds the result returned by <see cref="IGuestService.CreateAsync"/>.
    /// Used by the Razor view to render error alerts if the operation fails.
    /// </summary>
    protected Result<GuestResponse.Create>? Result { get; set; }

    /// <summary>
    /// Flag indicating whether the submission is currently in flight.
    /// Disables the submit button to prevent accidental double-clicks.
    /// </summary>
    protected bool IsSubmitting { get; set; }

    /// <summary>
    /// Called when the user submits the form and all client-side FluentValidation rules pass.
    /// Calls the guest service and navigates home upon success.
    /// </summary>
    protected async Task CreateGuestAsync()
    {
        IsSubmitting = true;
        try
        {
            Result = await GuestService.CreateAsync(Model);

            if (Result.IsSuccess)
            {
                // Navigate back to the home page or confirmation page
                NavigationManager.NavigateTo("/");
            }
        }
        finally
        {
            IsSubmitting = false;
        }
    }
}
