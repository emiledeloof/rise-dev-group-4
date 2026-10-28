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
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }

        // min nb of nights a guest must book, 1 by default
        public int MinNights { get; set; } = 1;

        // ! non refundable by default, the admin checks the box to make it refundable
        public bool IsRefundable { get; set; }

        // refund values, only used for a refundable rate, the client rules by default
        public int FullRefundDays { get; set; } = 30;
        public int PartialRefundDays { get; set; } = 7;
        public int PartialRefundPercent { get; set; } = 50;

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

                // same rules as the edit form, RatePricingRequest.Edit
                RuleFor(x => x.MinNights)
                    .GreaterThan(0)
                    .WithMessage("A stay must be at least 1 night.");

                // ! refund rules only for a refundable rate
                RuleFor(x => x.PartialRefundPercent)
                    .InclusiveBetween(0, 100)
                    .WithMessage("The partial refund must be between 0 and 100%.")
                    .When(x => x.IsRefundable);

                RuleFor(x => x.FullRefundDays)
                    .GreaterThan(x => x.PartialRefundDays)
                    .WithMessage("The full refund period must be longer than the partial one.")
                    .When(x => x.IsRefundable);

                RuleFor(x => x.PartialRefundDays)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("The number of days can't be negative.")
                    .When(x => x.IsRefundable);

                RuleFor(x => x.EndDate)
                    .Must((request, endDate) =>
                        request.StartDate is null ||
                        endDate is null ||
                        request.StartDate.Value < endDate.Value)
                    .WithMessage("Start date must be before end date.");
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
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
    }
}