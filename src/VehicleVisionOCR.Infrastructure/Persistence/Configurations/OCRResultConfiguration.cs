using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleVisionOCR.Domain.Entities;

namespace VehicleVisionOCR.Infrastructure.Persistence.Configurations
{
    public class OCRResultConfiguration : IEntityTypeConfiguration<OCRResult>
    {
        public void Configure(EntityTypeBuilder<OCRResult> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.FieldName).HasMaxLength(100);
            builder.Property(x => x.ExtractedValue).HasColumnType("TEXT");
            builder.Property(x => x.Status).HasConversion<string>();

            // Value Objects
            builder.OwnsOne(x => x.Confidence, c => c.Property(p => p.Percentage).HasColumnName("ConfidenceScore"));
        }
    }
}
