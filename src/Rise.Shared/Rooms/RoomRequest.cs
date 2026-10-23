namespace Rise.Shared.Rooms;

/// <summary>
/// requests used by the room endpoints, same idea as SearchRequest
/// </summary>
public static partial class RoomRequest
{
    /// <summary>
    /// how many rooms the list endpoint sends back, eg the landing page asks for 3
    /// </summary>
    public class List
    {
        /// <summary>
        /// max nb of rooms in the answer, 3 by default, one row of cards on the landing page
        /// </summary>
        public int Take { get; set; } = 3;

        /// <summary>
        /// query string of the request, eg ?take=3, same idea as QueryRequest.SkipTake
        /// </summary>
        public override string ToString() => $"?take={Take}";

        /// <summary>
        /// rules for a valid list request, same rules on the client and on the server
        /// </summary>
        public class Validator : AbstractValidator<List>
        {
            /// <summary>
            /// sets up the take rule
            /// </summary>
            public Validator()
            {
                // at least 1 room, and never more than 20 so a call can't ask for the whole db
                RuleFor(x => x.Take)
                    .InclusiveBetween(1, 20)
                    .WithMessage("Take must be between 1 and 20.");
            }
        }
    }
}
