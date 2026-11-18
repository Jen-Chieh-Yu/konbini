using Konbini.Api.Features.Products.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konbini.Api.Features.Common.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        // 檢查約束是最後一道防線：程式寫錯也存不進負數的庫存。條件只能寫成 SQL 字串，EF 沒有對應的 LINQ
        builder.ToTable("products", t =>
        {
            t.HasComment("商品（前台販售的零食、泡麵、飲品）");
            t.HasCheckConstraint("ck_products_stock", "stock >= 0");
        });
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasComment("商品編號");
        builder.Property(p => p.Type).HasComment("類別（1 零食、2 泡麵、3 飲品）");
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired().HasComment("品名");
        builder.Property(p => p.Price).HasComment("單價（新台幣整數）");
        builder.Property(p => p.ImageUrl).HasMaxLength(500).HasComment("圖片網址");
        builder.Property(p => p.Stock).HasComment("庫存（下單時扣除，不可小於 0）");
        builder.HasIndex(p => p.Type);
        builder.HasIndex(p => p.Status);
        builder.HasQueryFilter(p => p.Status != EntityStatus.Deleted);
    }
}
