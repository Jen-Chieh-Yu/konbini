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
        builder.Property(o => o.ContactName).HasMaxLength(100).IsRequired();
        builder.Property(o => o.ContactPhone).HasMaxLength(20).IsRequired();
        builder.Property(o => o.StreetAddress).HasMaxLength(200).IsRequired();
        builder.Property(o => o.DeliveryAddress).HasMaxLength(300).IsRequired();
        builder.Property(o => o.Memo).HasMaxLength(500);
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
