namespace VehicleVisionOCR.Domain.Enums
{
    /// <summary>
    /// Represents the synchronization status of an entity with the backend API.
    /// </summary>
    public enum SyncStatus
    {
        Pending = 0,
        InProgress = 1,
        Synced = 2,
        Failed = 3
    }
}
