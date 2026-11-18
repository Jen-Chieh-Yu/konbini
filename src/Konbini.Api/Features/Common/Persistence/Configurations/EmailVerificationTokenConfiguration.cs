using Konbini.Api.Features.Auth.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konbini.Api.Features.Common.Persistence.Configurations;

public class EmailVerificationTokenConfiguration : IEntityTypeConfiguration<EmailVerificationToken>
{
    public void Configure(EntityTypeBuilder<EmailVerificationToken> builder)
    {
        builder.ToTable("email_verification_tokens", t => t.HasComment("Email 驗證 token（註冊與重寄驗證信時發出，效期 24 小時）"));
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasComment("編號");
        builder.Property(t => t.UserId).HasComment("會員編號");
        builder.Property(t => t.TokenHash).HasMaxLength(EmailVerificationToken.HashLength).IsFixedLength().IsRequired()
            .HasComment("token 雜湊值（SHA-256，原文只出現在信中）");
        builder.Property(t => t.ExpiresOn).HasComment("到期時間（UTC，發出後 24 小時）");
        builder.Property(t => t.UsedOn).HasComment("使用時間（UTC；null 表示還沒用過）");
        builder.HasIndex(t => t.TokenHash).IsUnique();
        // 刪除使用者時一起刪掉 token（目前只有軟刪除，實際上不會觸發）
        builder.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
        // users 有軟刪除的過濾條件，這裡同步過濾：已刪除帳號的 token 查不到，EF 也不會警告必要關聯被過濾
        builder.HasQueryFilter(t => t.Status != EntityStatus.Deleted && t.User.Status != EntityStatus.Deleted);
    }
}
