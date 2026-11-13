using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Auth;
using Konbini.Api.Features.Common.Web;
using Konbini.Api.Features.Auth.Commands;
using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Auth.Queries;

namespace Konbini.Api.Features.Auth.Endpoints;

public class AuthEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (
                RegisterRequest request,
                ICommandHandler<RegisterCommand, Result> handler,
                CancellationToken ct)
            =>
            {
                var result = await handler.Handle(new(request), ct);
                return result.IsSuccess ? Results.NoContent() : result.Error.ToProblem();
            })
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        // 未實作登入速率限制：本專案為學習用途、不正式上線（CLAUDE.md §6）。
        // 若日後對外開放，這是上線前必補項（暴力破解防護）。
        group.MapPost("/login", async (
                LoginRequest request,
                ICommandHandler<LoginCommand, Result<LoginResponse>> handler,
                CancellationToken ct)
            =>
            {
                var result = await handler.Handle(new(request), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();
            })
            .Produces<LoginResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        // 信中的連結指向前端頁面，由頁面送這個 POST：信箱的連結掃描器會先開啟 GET 連結，不能讓 GET 改狀態
        group.MapPost("/email-verifications", async (
                VerifyEmailRequest request,
                ICommandHandler<VerifyEmailCommand, Result<VerifyEmailResponse>> handler,
                CancellationToken ct)
            =>
            {
                var result = await handler.Handle(new(request), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();
            })
            .Produces<VerifyEmailResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        // Email 不存在、已驗證、冷卻中都回 204，回應一致（見 ResendVerificationEmailHandler）
        group.MapPost("/verification-emails", async (
                ResendVerificationEmailRequest request,
                ICommandHandler<ResendVerificationEmailCommand, Result> handler,
                CancellationToken ct)
            =>
            {
                var result = await handler.Handle(new(request), ct);
                return result.IsSuccess ? Results.NoContent() : result.Error.ToProblem();
            })
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/password", async (
                ChangePasswordRequest request,
                ICurrentUser currentUser,
                ICommandHandler<ChangePasswordCommand, Result> handler,
                CancellationToken ct)
            =>
            {
                var result = await handler.Handle(new(currentUser.Id, request), ct);
                return result.IsSuccess ? Results.NoContent() : result.Error.ToProblem();
            })
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        group.MapGet("/me", async (
                ICurrentUser currentUser,
                IQueryHandler<GetCurrentUserQuery, Result<UserDto>> handler,
                CancellationToken ct)
            =>
            {
                var result = await handler.Handle(new(currentUser.Id), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();
            })
            .Produces<UserDto>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();
    }
}
