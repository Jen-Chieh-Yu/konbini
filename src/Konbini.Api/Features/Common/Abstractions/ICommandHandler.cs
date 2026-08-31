namespace Konbini.Api.Features.Common.Abstractions;

/// <summary>改變狀態的用例（Command）之處理器。</summary>
public interface ICommandHandler<in TCommand, TResult>
{
    Task<TResult> Handle(TCommand command, CancellationToken ct);
}
