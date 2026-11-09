namespace Konbini.Api.Features.Common.Persistence;

public enum EntityStatus : short
{
    Active = 1,
    Deleted = 9,
}

/// <summary>業務資料表的共用欄位，由 AuditInterceptor 填入；代碼表不繼承。</summary>
public abstract class AuditableEntity
{
    public DateTime CreatedOn { get; set; }

    /// <summary>null 表示沒有登入身分（註冊、種子資料）。</summary>
    public int? CreatedBy { get; set; }

    public DateTime ModifiedOn { get; set; }
    public int? ModifiedBy { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;
}
