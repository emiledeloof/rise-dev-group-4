namespace Rise.Shared.Rooms;

public static partial class RoomRequest
{
    /// <summary>new physical room, sent when the admin saves the create room form</summary>
    public class Create
    {
        /// <summary>room number shown on the door, for example 101</summary>
        public required string RoomNumber { get; set; }

        /// <summary>id of the room type chosen in the dropdown, 0 when nothing is chosen</summary>
        public int RoomTypeId { get; set; }

        /// <summary>maximum number of guests in the room</summary>
        // most rooms of the estate are for 2 guests
        public int MaxGuests { get; set; } = 2;

        /// <summary>checked on the client for feedback, then again on the server</summary>
        public class Validator : AbstractValidator<Create>
        {
            /// <summary>one rule per field, with a message that says which field is wrong</summary>
            public Validator()
            {
                // max 20 characters like the room number column
                RuleFor(x => x.RoomNumber)
                    .NotEmpty()
                    .WithMessage("Room number is required.")
                    .MaximumLength(20)
                    .WithMessage("Room number cannot exceed 20 characters.");

                // 0 means no room type was chosen
                RuleFor(x => x.RoomTypeId)
                    .GreaterThan(0)
                    .WithMessage("Select a room type.");

                RuleFor(x => x.MaxGuests)
                    .GreaterThanOrEqualTo(1)
                    .WithMessage("A room must have at least 1 guest.");
            }
        }
    }
}
