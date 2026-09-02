namespace Konbini.Api.Features.Common.Abstractions;

/// <summary>
/// Repository 的 marker 介面：實作類別經組件掃描自動以其非 marker 介面
/// 註冊進 DI（Scoped），加檔案不改 Program.cs（見 EndpointExtensions.AddRepositories）。
/// </summary>
public interface IRepository;
