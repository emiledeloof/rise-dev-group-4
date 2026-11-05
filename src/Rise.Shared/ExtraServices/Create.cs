using FluentValidation;

namespace Rise.Shared.ExtraServices;

/// <summary>
/// Requests sent by the extra services management page.
/// </summary>
public static partial class ExtraServiceRequest
{
    /// <summary>
    /// Shared form fields for creating and editing extra services.
    /// </summary>
    public abstract class Form
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public ChargeCadence Cadence { get; set; } = ChargeCadence.PerNight;
        public bool IsPerPerson { get; set; }
    }

    /// <summary>
    /// Validation rules shared between Create and Edit requests.
    /// </summary>
    public abstract class FormValidator<TForm> : AbstractValidator<TForm> where TForm : Form
    {
        protected FormValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Service name is required.")
                .MaximumLength(100)
                .WithMessage("Service name cannot exceed 100 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(1000)
                .WithMessage("Description cannot exceed 1000 characters.");

            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Price must be 0 or greater.");
        }
    }

    /// <summary>
    /// Request to create a new extra service.
    /// </summary>
    public class Create : Form
    {
        public class Validator : FormValidator<Create>
        {
        }
    }
}

/// <summary>
/// Response after creating an extra service.
/// </summary>
public static partial class ExtraServiceResponse
{
    public class Create
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
    }
}
