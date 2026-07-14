using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleVisionOCR.Domain.Entities;

namespace VehicleVisionOCR.Infrastructure.Persistence.Configurations
{
    public class ScannerDeviceConfiguration : IEntityTypeConfiguration<ScannerDevice>
    {
        public void Configure(EntityTypeBuilder<ScannerDevice> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
            builder.Property(x => x.SerialNumber).HasMaxLength(100);
            builder.Property(x => x.Type).HasConversion<string>();
            builder.Property(x => x.Status).HasConversion<string>();
        }
    }
}
