using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleVisionOCR.Domain.Entities;

namespace VehicleVisionOCR.Infrastructure.Persistence.Configurations
{
    public class ErrorLogConfiguration : IEntityTypeConfiguration<ErrorLog>
    {
        public void Configure(EntityTypeBuilder<ErrorLog> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ErrorMessage).IsRequired().HasColumnType("TEXT");
            builder.Property(x => x.StackTrace).HasColumnType("TEXT");
            builder.Property(x => x.Source).HasMaxLength(255);
            
            builder.HasIndex(x => x.OccurredAt);
        }
    }
}
