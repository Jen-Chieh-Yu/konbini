using Konbini.Api.Features.Auth.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konbini.Api.Features.Common.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", t => t.HasComment("會員（註冊的帳號；Email 驗證完成才能登入）"));
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasComment("會員編號");
        builder.Property(u => u.Name).HasMaxLength(User.NameMaxLength).IsRequired().HasComment("姓名");
        builder.Property(u => u.Email).HasMaxLength(User.EmailMaxLength).IsRequired()
            .HasComment("Email（小寫正規化後儲存，不可重複）");
        builder.Property(u => u.PasswordHash).HasMaxLength(300).IsRequired().HasComment("密碼雜湊值（不存明碼）");
        builder.Property(u => u.Birthday).HasComment("生日");
        builder.Property(u => u.Phone).HasMaxLength(20).HasComment("電話");
        builder.Property(u => u.EmailVerifiedOn).HasComment("Email 驗證完成時間（UTC；null 表示尚未驗證，不能登入）");
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasQueryFilter(u => u.Status != EntityStatus.Deleted);
    }
}
