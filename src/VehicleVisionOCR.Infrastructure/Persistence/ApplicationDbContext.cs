using Microsoft.EntityFrameworkCore;
using VehicleVisionOCR.Domain.Entities;
using System.Reflection;

namespace VehicleVisionOCR.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<VehicleColor> VehicleColors => Set<VehicleColor>();
        public DbSet<VehicleScan> VehicleScans => Set<VehicleScan>();
        public DbSet<ScanImage> ScanImages => Set<ScanImage>();
        public DbSet<OCRResult> OCRResults => Set<OCRResult>();
        public DbSet<PendingSync> PendingSyncs => Set<PendingSync>();
        public DbSet<ApplicationSetting> ApplicationSettings => Set<ApplicationSetting>();
        public DbSet<ScannerDevice> ScannerDevices => Set<ScannerDevice>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            base.OnModelCreating(modelBuilder);
        }
    }
}
