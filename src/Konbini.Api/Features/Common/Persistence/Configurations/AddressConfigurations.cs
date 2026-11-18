using Konbini.Api.Features.Addresses.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konbini.Api.Features.Common.Persistence.Configurations;

public class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("cities", t => t.HasComment("縣市（結帳頁的縣市選項；代碼表）"));
        builder.HasKey(c => c.CityCode);
        builder.Property(c => c.CityCode).ValueGeneratedNever().HasComment("縣市代碼");
        builder.Property(c => c.CityName).HasMaxLength(50).IsRequired().HasComment("縣市名稱");
    }
}

public class DistrictConfiguration : IEntityTypeConfiguration<District>
{
    public void Configure(EntityTypeBuilder<District> builder)
    {
        builder.ToTable("districts", t => t.HasComment("行政區（結帳頁的行政區選項，依縣市分組；代碼表）"));
        builder.HasKey(d => d.DistrictCode);
        builder.Property(d => d.CityCode).HasComment("縣市代碼");
        builder.Property(d => d.DistrictCode).ValueGeneratedNever().HasComment("行政區代碼");
        builder.Property(d => d.DistrictName).HasMaxLength(50).IsRequired().HasComment("行政區名稱");
        builder.HasIndex(d => d.CityCode);
    }
}
