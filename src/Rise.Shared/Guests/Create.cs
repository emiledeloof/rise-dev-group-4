namespace Rise.Shared.Guests;

/// <summary>
/// Static partial class grouping all request DTOs for guests.
/// Allows future operations (e.g. Edit, Delete, Index) to be added in separate files under the same namespace.
/// </summary>
public static partial class GuestRequest
{
    /// <summary>
    /// Represents the input data required to register/create a new guest.
    /// This DTO is used both for form binding in Blazor and as the HTTP request body sent to the API.
    /// </summary>
    public class Create
    {
        /// <summary>
        /// The guest's first name.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// The guest's last name.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// The guest's email address (used for booking confirmations; unique in the database).
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// The guest's phone number.
        /// </summary>
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// The primary address line (e.g., street and house number).
        /// </summary>
        public string AddressLine1 { get; set; } = string.Empty;

        /// <summary>
        /// Optional secondary address line (e.g., apartment, suite, floor).
        /// </summary>
        public string? AddressLine2 { get; set; }

        /// <summary>
        /// The country of residence (constrained to max 100 characters in the database).
        /// </summary>
        public string Country { get; set; } = string.Empty;

        /// <summary>
        /// FluentValidation validator for <see cref="Create"/>.
        /// Automatically discovered by FastEndpoints on the server,
        /// and shared directly with the Blazor client via &lt;FluentValidationValidator /&gt;.
        /// </summary>
        public class Validator : AbstractValidator<Create>
        {
            public Validator()
            {
                // First name: required, max 120 characters (FirstName + LastName fits within DB column max 250)
                RuleFor(x => x.FirstName)
                    .NotEmpty().WithMessage("First name is required.")
                    .MaximumLength(120).WithMessage("First name must not exceed 120 characters.");

                // Last name: required, max 120 characters
                RuleFor(x => x.LastName)
                    .NotEmpty().WithMessage("Last name is required.")
                    .MaximumLength(120).WithMessage("Last name must not exceed 120 characters.");

                // Email: required, valid email format, max 250 characters
                RuleFor(x => x.Email)
                    .NotEmpty().WithMessage("Email address is required.")
                    .EmailAddress().WithMessage("Please enter a valid email address.")
                    .MaximumLength(250).WithMessage("Email address must not exceed 250 characters.");

                // Phone: required, max 50 characters (matches column limit in GuestConfiguration)
                RuleFor(x => x.PhoneNumber)
                    .NotEmpty().WithMessage("Phone number is required.")
                    .MaximumLength(50).WithMessage("Phone number must not exceed 50 characters.");

                // AddressLine1: required, max 250 characters
                RuleFor(x => x.AddressLine1)
                    .NotEmpty().WithMessage("Address line 1 is required.")
                    .MaximumLength(250).WithMessage("Address line 1 must not exceed 250 characters.");

                // AddressLine2: optional, but cannot exceed 250 characters if provided
                RuleFor(x => x.AddressLine2)
                    .MaximumLength(250).WithMessage("Address line 2 must not exceed 250 characters.")
                    .When(x => !string.IsNullOrWhiteSpace(x.AddressLine2));

                // Country: required, max 100 characters (matches column limit in GuestConfiguration)
                RuleFor(x => x.Country)
                    .NotEmpty().WithMessage("Country is required.")
                    .MaximumLength(100).WithMessage("Country must not exceed 100 characters.");
            }
        }
    }
}

/// <summary>
/// Static partial class grouping all response DTOs for guests.
/// </summary>
public static partial class GuestResponse
{
    /// <summary>
    /// Response payload returned after successfully creating a new guest.
    /// Returns the database-generated ID.
    /// </summary>
    public class Create
    {
        /// <summary>
        /// The database ID of the newly created guest.
        /// </summary>
        public int GuestId { get; set; }
    }
}
