using Konbini.Api.Features.Captcha.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Konbini.Api.Features.Captcha.Services;

/// <summary>驗證碼的答案存放處。答案只能比對一次，答錯也作廢。</summary>
public interface ICaptchaStore
{
    /// <summary>存入答案，回傳之後比對用的 id。</summary>
    string Save(string answer);

    /// <summary>比對並作廢：不論對錯，這個 id 之後都不能再用。</summary>
    bool TryConsume(string? id, string answer);
}

/// <summary>
/// 存在記憶體：API 只有一個實例（CLAUDE.md §3），重啟後未使用的驗證碼失效可以接受。
/// 用自己的 MemoryCache，筆數上限不影響其他快取；滿了就存不進去，那張驗證碼會被當成過期。
/// </summary>
public sealed class MemoryCaptchaStore(TimeProvider time) : ICaptchaStore, IDisposable
{
    /// <summary>2 分鐘內最多存幾筆；每筆只有 id 與 5 個字元。</summary>
    public const int MaxEntries = 10_000;

    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = MaxEntries });
    private readonly Lock _gate = new();

    public string Save(string answer)
    {
        var id = Guid.NewGuid().ToString("N");
        var expiresAt = time.GetUtcNow() + CaptchaOptions.Lifetime;
        // 到期時間另外存一份，比對時用 TimeProvider 判斷（測試可以換時間）；快取的到期只負責清掉記憶體
        _cache.Set(id, new Entry(CaptchaCode.Normalize(answer), expiresAt),
            new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = CaptchaOptions.Lifetime });
        return id;
    }

    public bool TryConsume(string? id, string answer)
    {
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }
        Entry? entry;
        // 同一個 id 同時送兩次時，只有一個請求拿得到答案
        lock (_gate)
        {
            if (!_cache.TryGetValue(id, out entry) || entry is null)
            {
                return false;
            }
            _cache.Remove(id);
        }
        return time.GetUtcNow() < entry.ExpiresAt && entry.Answer == CaptchaCode.Normalize(answer);
    }

    public void Dispose() => _cache.Dispose();

    private sealed record Entry(string Answer, DateTimeOffset ExpiresAt);
}
