using Konbini.Api.Features.Auth.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Konbini.Api.Features.Common.Persistence.Configurations;

public class EmailVerificationTokenConfiguration : IEntityTypeConfiguration<EmailVerificationToken>
{
    public void Configure(EntityTypeBuilder<EmailVerificationToken> builder)
    {
        builder.ToTable("email_verification_tokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TokenHash).HasMaxLength(EmailVerificationToken.HashLength).IsFixedLength().IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique();
        // 刪除使用者時一起刪掉 token（目前只有軟刪除，實際上不會觸發）
        builder.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
        // users 有軟刪除的過濾條件，這裡同步過濾：已刪除帳號的 token 查不到，EF 也不會警告必要關聯被過濾
        builder.HasQueryFilter(t => t.Status != EntityStatus.Deleted && t.User.Status != EntityStatus.Deleted);
    }
}
