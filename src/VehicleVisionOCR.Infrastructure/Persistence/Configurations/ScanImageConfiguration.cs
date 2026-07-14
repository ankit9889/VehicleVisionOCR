using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleVisionOCR.Domain.Entities;

namespace VehicleVisionOCR.Infrastructure.Persistence.Configurations
{
    public class ScanImageConfiguration : IEntityTypeConfiguration<ScanImage>
    {
        public void Configure(EntityTypeBuilder<ScanImage> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.LocalFilePath).IsRequired().HasMaxLength(500);
            builder.Property(x => x.RemoteUrl).HasMaxLength(500);
            
            builder.HasOne(x => x.VehicleScan)
                   .WithMany(s => s.Images)
                   .HasForeignKey(x => x.VehicleScanId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
