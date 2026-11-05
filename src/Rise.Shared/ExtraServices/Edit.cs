using FluentValidation;

namespace Rise.Shared.ExtraServices;

public static partial class ExtraServiceRequest
{
    /// <summary>
    /// Request to update an existing extra service.
    /// </summary>
    public class Edit : Form
    {
        public int Id { get; set; }
        public bool IsActive { get; set; } = true;

        public class Validator : FormValidator<Edit>
        {
            public Validator()
            {
                RuleFor(x => x.Id).GreaterThan(0).WithMessage("Valid service ID is required.");
            }
        }
    }
}
