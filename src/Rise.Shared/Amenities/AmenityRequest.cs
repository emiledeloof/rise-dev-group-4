using Rise.Shared.Common;

namespace Rise.Shared.Amenities;

/// <summary>
/// requests sent by the admin amenities page
/// </summary>
public static class AmenityRequest
{
    /// <summary>
    /// fields of the create and edit forms, written once for both
    /// </summary>
    public abstract class Form
    {
        /// <summary>
        /// name per language code, English is required, an empty name means no translation
        /// </summary>
        public Dictionary<string, string?> Names { get; set; } = [];

        /// <summary>
        /// Font Awesome icon, eg fa-wifi
        /// </summary>
        public string Icon { get; set; } = string.Empty;
    }

    /// <summary>
    /// rules shared by the create and edit forms
    /// </summary>
    // * abstract so fastendpoints only picks the validators of the real requests
    public abstract class FormValidator<TForm> : AbstractValidator<TForm> where TForm : Form
    {
        /// <summary>
        /// same limits as the amenity tables
        /// </summary>
        protected FormValidator()
        {
            RuleFor(x => x.Names)
                .Must(names => names.TryGetValue(Languages.Default, out var name) && !string.IsNullOrWhiteSpace(name))
                .WithMessage("A name in English is required.")
                .Must(names => names.Keys.All(Languages.IsSupported))
                .WithMessage("Only the languages of the site can be used.")
                .Must(names => names.Values.All(name => name is null || name.Trim().Length <= 100))
                .WithMessage("A name cannot exceed 100 characters.");

            RuleFor(x => x.Icon)
                .NotEmpty()
                .WithMessage("An icon is required.")
                .MaximumLength(50)
                .WithMessage("The icon cannot exceed 50 characters.")
                .Matches("^fa-[a-z0-9-]+$")
                .WithMessage("The icon must be a Font Awesome name, for example fa-wifi.");
        }
    }

    /// <summary>
    /// new amenity, sent when the admin saves the create form
    /// </summary>
    public class Create : Form
    {
        /// <summary>
        /// checked on the client for feedback, then again on the server
        /// </summary>
        public class Validator : FormValidator<Create>
        {
        }
    }

    /// <summary>
    /// new names and icon of an existing amenity
    /// </summary>
    public class Edit : Form
    {
        /// <summary>
        /// id of the amenity to change
        /// </summary>
        public int AmenityId { get; set; }

        /// <summary>
        /// form rules plus a real id
        /// </summary>
        public class Validator : FormValidator<Edit>
        {
            /// <summary>
            /// adds the id rule to the shared form rules
            /// </summary>
            public Validator()
            {
                RuleFor(x => x.AmenityId).GreaterThan(0);
            }
        }
    }
}

/// <summary>
/// answers of the amenity endpoints
/// </summary>
public static class AmenityResponse
{
    /// <summary>
    /// all amenities for the admin page, active and inactive, no paging because the estate only has a few
    /// </summary>
    public class Index
    {
        /// <summary>
        /// amenities sorted by English name
        /// </summary>
        public required IReadOnlyList<AmenityDto.Admin> Amenities { get; init; }
    }

    /// <summary>
    /// the amenity that was just created
    /// </summary>
    public class Create
    {
        /// <summary>
        /// id given by the database
        /// </summary>
        public required int Id { get; set; }

        /// <summary>
        /// English name as saved
        /// </summary>
        public required string Name { get; set; }
    }
}
