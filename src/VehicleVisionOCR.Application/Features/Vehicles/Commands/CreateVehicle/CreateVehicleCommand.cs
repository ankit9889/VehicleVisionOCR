using System;
using MediatR;
using VehicleVisionOCR.Application.Common.Models;

namespace VehicleVisionOCR.Application.Features.Vehicles.Commands.CreateVehicle
{
    public class CreateVehicleCommand : IRequest<Result<Guid>>
    {
        public string Vin { get; set; } = string.Empty;
        public string? RegistrationNumber { get; set; }
    }
}
