using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleVisionOCR.Domain.Entities;

namespace VehicleVisionOCR.Infrastructure.Persistence.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Action).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Type).HasConversion<string>();
            builder.Property(x => x.Message).IsRequired().HasColumnType("TEXT");
            builder.Property(x => x.Details).HasColumnType("TEXT");
            
            builder.HasIndex(x => x.Timestamp);
        }
    }
}
