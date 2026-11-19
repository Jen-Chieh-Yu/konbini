using Konbini.Api.Features.Products.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konbini.Api.Features.Common.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        // 檢查約束是最後一道防線：程式寫錯也存不進負數的庫存。條件只能寫成 SQL 字串，EF 沒有對應的 LINQ
        builder.ToTable("products", t => t.HasCheckConstraint("ck_products_stock", "stock >= 0"));
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.ImageUrl).HasMaxLength(500);
        builder.HasIndex(p => p.Type);
        builder.HasIndex(p => p.Status);
        builder.HasQueryFilter(p => p.Status != EntityStatus.Deleted);
    }
}
