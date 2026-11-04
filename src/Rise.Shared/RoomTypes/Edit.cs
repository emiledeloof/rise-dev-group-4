namespace Rise.Shared.RoomTypes;

public static partial class RoomTypeRequest
{
    /// <summary>
    /// new values of an existing room type, sent when the admin saves the edit form
    /// </summary>
    public class Edit : Form
    {
        /// <summary>
        /// id of the room type to change
        /// </summary>
        public int RoomTypeId { get; set; }

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
                RuleFor(x => x.RoomTypeId).GreaterThan(0);
            }
        }
    }
}
