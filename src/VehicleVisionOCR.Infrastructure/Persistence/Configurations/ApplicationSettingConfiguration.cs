using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleVisionOCR.Domain.Entities;

namespace VehicleVisionOCR.Infrastructure.Persistence.Configurations
{
    public class ApplicationSettingConfiguration : IEntityTypeConfiguration<ApplicationSetting>
    {
        public void Configure(EntityTypeBuilder<ApplicationSetting> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Key).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Value).HasColumnType("TEXT");
            
            builder.HasIndex(x => x.Key).IsUnique();
        }
    }
}
