namespace Rise.Shared.Bookings;

public static partial class BookingRequest
{
    public class Create
    {
        public int GuestId { get; set; }
        public DateOnly CheckIn { get; set; }
        public DateOnly CheckOut { get; set; }
        public List<int> RoomIds { get; set; } = [];
        
        public class Validator : AbstractValidator<Create>
        {
            public Validator()
            {
                RuleFor(x => x.GuestId).GreaterThan(0).NotEmpty();
                RuleFor(x => x.CheckIn).NotEmpty();
                RuleFor(x => x.CheckOut).NotEmpty();
                RuleFor(x => x.CheckOut)
                    .GreaterThan(x => x.CheckIn)
                    .WithMessage("Check-out date must be after check-in date.");
                RuleFor(x => x.RoomIds).NotEmpty();
            }
        }
    }
}

public static partial class BookingResponse
{
    public class Create
    {
        public int BookingId { get; set; }
        public string BookingNumber { get; set; } = string.Empty;
    }
}