using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleVisionOCR.Domain.Entities;

namespace VehicleVisionOCR.Infrastructure.Persistence.Configurations
{
    public class VehicleScanConfiguration : IEntityTypeConfiguration<VehicleScan>
    {
        public void Configure(EntityTypeBuilder<VehicleScan> builder)
        {
            builder.HasKey(x => x.Id);
            
            builder.Property(x => x.Status).HasConversion<string>();

            builder.HasOne(x => x.Vehicle)
                   .WithMany(v => v.Scans)
                   .HasForeignKey(x => x.VehicleId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.ScannerDevice)
                   .WithMany()
                   .HasForeignKey(x => x.ScannerDeviceId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(x => x.OCRResults)
                   .WithOne(o => o.VehicleScan)
                   .HasForeignKey(o => o.VehicleScanId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
