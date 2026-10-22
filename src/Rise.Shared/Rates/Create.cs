using FluentValidation.Validators;

namespace Rise.Shared.Rates;

public static partial class RateRequest
{
    public class Create
    {
        public required string Name { get; set; }
        public required decimal PricePerNight { get; set; }
        public string? Description { get; set; }
        public required string RoomName { get; set; }

        public class Validator : AbstractValidator<Create>
        {
            public Validator()
            {
                RuleFor(x => x.Name)
                    .NotEmpty()
                    .WithMessage("Rate name is required.")
                    .MaximumLength(100)
                    .WithMessage("Rate name cannot exceed 100 characters.");

                RuleFor(x => x.PricePerNight)
                    .GreaterThan(0)
                    .WithMessage("Price per night must be greater than 0.")
                    .PrecisionScale(18, 2, true)
                    .WithMessage("Price per night can have at most 2 decimal places.");

                RuleFor(x => x.Description)
                    .NotEmpty()
                    .WithMessage("Cancellation policy / description is required.")
                    .MaximumLength(500)
                    .WithMessage("Cancellation policy / description cannot exceed 500 characters.");

                RuleFor(x => x.RoomName)
                    .NotEmpty()
                    .WithMessage("Select a room type.");
            }
        }
    }
}

public static partial class RateResponse
{
    public class Create
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public required decimal PricePerNight { get; set; }
        public string? Description { get; set; }
        public required string RoomName { get; set; }
    }
}