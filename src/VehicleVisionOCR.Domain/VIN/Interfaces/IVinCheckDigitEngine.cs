namespace VehicleVisionOCR.Domain.VIN.Interfaces
{
    public interface IVinCheckDigitEngine
    {
        bool Calculate(string vin);
    }
}
