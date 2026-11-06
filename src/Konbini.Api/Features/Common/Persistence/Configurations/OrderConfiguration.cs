using Konbini.Api.Features.Orders.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konbini.Api.Features.Common.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.ContactName).HasMaxLength(Order.ContactNameMaxLength).IsRequired();
        builder.Property(o => o.ContactPhone).HasMaxLength(Order.ContactPhoneMaxLength).IsRequired();
        builder.Property(o => o.StreetAddress).HasMaxLength(Order.StreetAddressMaxLength).IsRequired();
        // 縣市（50）＋行政區（50）＋街道地址（200）的組合，上限足夠，不需另外驗證
        builder.Property(o => o.DeliveryAddress).HasMaxLength(300).IsRequired();
        builder.Property(o => o.Memo).HasMaxLength(Order.MemoMaxLength);
        builder.HasIndex(o => o.UserId);
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
        builder.ToTable("order_items");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(i => i.ImageUrl).HasMaxLength(500);
    }
}
