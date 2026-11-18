using Konbini.Api.Features.Orders.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konbini.Api.Features.Common.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders", t => t.HasComment("訂單（會員送出的訂單，金額由後端重算；寄送地址是下單當時的快照）"));
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasComment("訂單編號");
        builder.Property(o => o.UserId).HasComment("下單會員編號");
        builder.Property(o => o.Subtotal).HasComment("小計（明細小計加總）");
        builder.Property(o => o.DeliveryFee).HasComment("運費（小計滿 500 為 0，未滿為 60）");
        builder.Property(o => o.Total).HasComment("合計（小計＋運費）");
        builder.Property(o => o.ContactName).HasMaxLength(Order.ContactNameMaxLength).IsRequired().HasComment("聯絡人姓名");
        builder.Property(o => o.ContactPhone).HasMaxLength(Order.ContactPhoneMaxLength).IsRequired().HasComment("聯絡電話");
        builder.Property(o => o.DeliveryMethod).HasComment("配送方式（尚未定義選項，前端固定送 1）");
        builder.Property(o => o.CityCode).HasComment("縣市代碼");
        builder.Property(o => o.DistrictCode).HasComment("行政區代碼");
        builder.Property(o => o.StreetAddress).HasMaxLength(Order.StreetAddressMaxLength).IsRequired()
            .HasComment("地址（不含縣市、行政區）");
        // 縣市（50）＋行政區（50）＋街道地址（200）的組合，上限足夠，不需另外驗證
        builder.Property(o => o.DeliveryAddress).HasMaxLength(300).IsRequired()
            .HasComment("寄送地址（縣市＋行政區＋地址，下單當時的快照）");
        builder.Property(o => o.Memo).HasMaxLength(Order.MemoMaxLength).HasComment("備註");
        builder.HasIndex(o => o.UserId);
        builder.HasIndex(o => o.CreatedOn);
        builder.HasQueryFilter(o => o.Status != EntityStatus.Deleted);
        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items", t => t.HasComment("訂單明細（一筆訂單的每項商品；品名、單價、圖片是下單當時的快照）"));
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasComment("明細編號");
        builder.Property(i => i.OrderId).HasComment("訂單編號");
        builder.Property(i => i.ProductId).HasComment("商品編號");
        builder.Property(i => i.ProductName).HasMaxLength(200).IsRequired().HasComment("品名（下單當時的快照）");
        builder.Property(i => i.UnitPrice).HasComment("單價（下單當時的快照）");
        builder.Property(i => i.Quantity).HasComment("數量（1–99）");
        builder.Property(i => i.Subtotal).HasComment("小計（單價 × 數量）");
        builder.Property(i => i.ImageUrl).HasMaxLength(500).HasComment("圖片網址（下單當時的快照）");
        builder.HasQueryFilter(i => i.Status != EntityStatus.Deleted);
    }
}
