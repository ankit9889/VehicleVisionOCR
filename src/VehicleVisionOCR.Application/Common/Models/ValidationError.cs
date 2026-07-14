namespace VehicleVisionOCR.Application.Common.Models
{
    /// <summary>
    /// Represents a single validation error in a request or operation.
    /// </summary>
    public class ValidationError
    {
        public string PropertyName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        
        public ValidationError(string propertyName, string errorMessage)
        {
            PropertyName = propertyName;
            ErrorMessage = errorMessage;
        }
    }
}
