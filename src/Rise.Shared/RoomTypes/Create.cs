using Rise.Shared.Common;

namespace Rise.Shared.RoomTypes;

/// <summary>
/// requests sent by the admin room types page
/// </summary>
public static partial class RoomTypeRequest
{
    /// <summary>
    /// fields of the create and edit forms, written once for both
    /// </summary>
    public abstract class Form
    {
        /// <summary>
        /// name of the type, for example deluxe room
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// what the type offers, can stay empty
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// ids of the active amenities the admin ticked, the inactive ones already assigned are kept by the server
        /// </summary>
        public List<int> AmenityIds { get; set; } = [];

        /// <summary>
        /// beds per language code, eg 2 single beds, an empty text means no translation
        /// </summary>
        public Dictionary<string, string?> BedConfigurations { get; set; } = [];
    }

    /// <summary>
    /// rules shared by the create and edit forms
    /// </summary>
    // * abstract so fastendpoints only picks the validators of the real requests
    public abstract class FormValidator<TForm> : AbstractValidator<TForm> where TForm : Form
    {
        /// <summary>
        /// same limits as the room type table
        /// </summary>
        protected FormValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Room type name is required.")
                .MaximumLength(100)
                .WithMessage("Room type name cannot exceed 100 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(1000)
                .WithMessage("Description cannot exceed 1000 characters.");

            RuleFor(x => x.AmenityIds)
                .Must(ids => ids.All(id => id > 0) && ids.Distinct().Count() == ids.Count)
                .WithMessage("The amenities are not valid.");

            RuleFor(x => x.BedConfigurations)
                .Must(beds => beds.Keys.All(Languages.IsSupported))
                .WithMessage("Only the languages of the site can be used.")
                .Must(beds => beds.Values.All(bed => bed is null || bed.Trim().Length <= 100))
                .WithMessage("A bed configuration cannot exceed 100 characters.");
        }
    }

    /// <summary>
    /// new room type, sent when the admin saves the create form
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
}

/// <summary>
/// answers of the room type endpoints
/// </summary>
public static partial class RoomTypeResponse
{
    /// <summary>
    /// the room type that was just created
    /// </summary>
    public class Create
    {
        /// <summary>
        /// id given by the database
        /// </summary>
        public required int Id { get; set; }

        /// <summary>
        /// name as saved, trimmed by the domain
        /// </summary>
        public required string Name { get; set; }
    }
}
