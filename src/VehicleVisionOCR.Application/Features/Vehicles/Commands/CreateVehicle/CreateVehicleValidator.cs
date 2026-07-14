using FluentValidation;

namespace VehicleVisionOCR.Application.Features.Vehicles.Commands.CreateVehicle
{
    public class CreateVehicleValidator : AbstractValidator<CreateVehicleCommand>
    {
        public CreateVehicleValidator()
        {
            RuleFor(x => x.Vin)
                .NotEmpty().WithMessage("VIN is required.")
                .Length(17).WithMessage("VIN must be exactly 17 characters.");
        }
    }
}
