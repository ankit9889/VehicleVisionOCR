using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleVisionOCR.Domain.Entities;

namespace VehicleVisionOCR.Infrastructure.Persistence.Configurations
{
    public class PendingSyncConfiguration : IEntityTypeConfiguration<PendingSync>
    {
        public void Configure(EntityTypeBuilder<PendingSync> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Payload).IsRequired().HasColumnType("TEXT");
            builder.Property(x => x.Endpoint).HasMaxLength(255);
            builder.Property(x => x.HttpMethod).HasMaxLength(10);
            builder.Property(x => x.Status).HasConversion<string>();
            
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.QueuedAt); // For ordering the queue
        }
    }
}
