using Konbini.Api.Features.Addresses.Models;
using Konbini.Api.Features.Orders.Models;
using Konbini.Api.Features.Products.Models;
using Konbini.Api.Features.Auth.Models;
using Microsoft.EntityFrameworkCore;

namespace Konbini.Api.Features.Common.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<User> Users => Set<User>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<District> Districts => Set<District>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // 共用欄位的說明只寫一次：所有繼承 AuditableEntity 的資料表套用同一組
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(e => typeof(AuditableEntity).IsAssignableFrom(e.ClrType)).ToList())
        {
            var entity = modelBuilder.Entity(entityType.ClrType);
            entity.Property(nameof(AuditableEntity.CreatedOn)).HasComment("建立時間（UTC）");
            entity.Property(nameof(AuditableEntity.CreatedBy)).HasComment("建立者（會員編號；null 表示沒有登入身分，例：註冊、種子資料）");
            entity.Property(nameof(AuditableEntity.ModifiedOn)).HasComment("最後修改時間（UTC）");
            entity.Property(nameof(AuditableEntity.ModifiedBy)).HasComment("最後修改者（會員編號；null 表示沒有登入身分）");
            entity.Property(nameof(AuditableEntity.Status)).HasComment("狀態（1 正常、9 已刪除；兼作軟刪除）");
        }
    }
}
