using VehicleVisionOCR.Domain.Common;

namespace VehicleVisionOCR.Domain.Entities
{
    /// <summary>
    /// Represents an authenticated operator or administrator.
    /// </summary>
    public class User : AuditableEntity
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string PasswordHash { get; set; } = string.Empty;
        public string? Role { get; set; }
    }
}
