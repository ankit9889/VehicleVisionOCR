using System.Threading.Tasks;
using VehicleVisionOCR.Application.Common.Models;
using VehicleVisionOCR.Domain.Entities;

namespace VehicleVisionOCR.Application.Interfaces
{
    /// <summary>
    /// Handles user authentication and JWT management.
    /// </summary>
    public interface IAuthenticationService
    {
        Task<Result<User>> LoginAsync(string username, string password);
        Task LogoutAsync();
        Task<User?> GetCurrentUserAsync();
        Task<bool> RefreshTokenAsync();
    }
}
