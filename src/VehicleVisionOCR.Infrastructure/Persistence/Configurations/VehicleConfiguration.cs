using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VehicleVisionOCR.Domain.Entities;

namespace VehicleVisionOCR.Infrastructure.Persistence.Configurations
{
    public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
    {
        public void Configure(EntityTypeBuilder<Vehicle> builder)
        {
            builder.HasKey(x => x.Id);

            // Configure Value Objects as Owned Types
            builder.OwnsOne(x => x.Vin, v =>
            {
                v.Property(p => p.Value).HasColumnName("Vin").IsRequired().HasMaxLength(17);
                v.HasIndex(p => p.Value).IsUnique();
            });

            builder.OwnsOne(x => x.RegistrationNumber, r =>
            {
                r.Property(p => p.Value).HasColumnName("RegistrationNumber").HasMaxLength(20);
                r.HasIndex(p => p.Value);
            });

            builder.OwnsOne(x => x.EngineNumber, e =>
            {
                e.Property(p => p.Value).HasColumnName("EngineNumber").HasMaxLength(50);
            });

            builder.OwnsOne(x => x.ChassisNumber, c =>
            {
                c.Property(p => p.Value).HasColumnName("ChassisNumber").HasMaxLength(50);
            });

            builder.Property(x => x.Make).HasMaxLength(100);
            builder.Property(x => x.Model).HasMaxLength(100);
            builder.Property(x => x.Type).HasConversion<string>();

            // Relationships
            builder.HasMany(x => x.Scans)
                   .WithOne(s => s.Vehicle)
                   .HasForeignKey(s => s.VehicleId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
