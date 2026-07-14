using System;
using System.Threading.Tasks;
using VehicleVisionOCR.Domain.Enums;

namespace VehicleVisionOCR.Application.Interfaces
{
    /// <summary>
    /// Service for creating structured database audit logs and error logs.
    /// </summary>
    public interface ILogService
    {
        Task LogAuditAsync(string action, string message, LogType type = LogType.Info, string? details = null);
        Task LogErrorAsync(Exception ex, string? customMessage = null);
    }
}
