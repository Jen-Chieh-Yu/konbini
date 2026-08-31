using Konbini.Api.Features.Addresses.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konbini.Api.Features.Common.Persistence.Configurations;

public class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("cities");
        builder.HasKey(c => c.CityCode);
        builder.Property(c => c.CityCode).ValueGeneratedNever();
        builder.Property(c => c.CityName).HasMaxLength(50).IsRequired();
    }
}

public class DistrictConfiguration : IEntityTypeConfiguration<District>
{
    public void Configure(EntityTypeBuilder<District> builder)
    {
        builder.ToTable("districts");
        builder.HasKey(d => d.DistrictCode);
        builder.Property(d => d.DistrictCode).ValueGeneratedNever();
        builder.Property(d => d.DistrictName).HasMaxLength(50).IsRequired();
        builder.HasIndex(d => d.CityCode);
    }
}
