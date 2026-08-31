namespace Konbini.Api.Features.Common.Abstractions;

/// <summary>唯讀用例（Query）之處理器：一律 AsNoTracking 並直接投影 DTO。</summary>
public interface IQueryHandler<in TQuery, TResult>
{
    Task<TResult> Handle(TQuery query, CancellationToken ct);
}
