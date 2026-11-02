using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Auth;
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
                ICommandHandler<RegisterCommand, AuthResult> handler,
                CancellationToken ct)
            =>
            {
                var result = await handler.Handle(new(request), ct);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .Produces<AuthResult>()
            .Produces<AuthResult>(StatusCodes.Status400BadRequest);

        // 未實作登入速率限制：本專案為學習用途、不正式上線（CLAUDE.md §6）。
        // 若日後對外開放，這是上線前必補項（暴力破解防護）。
        group.MapPost("/login", async (
                LoginRequest request,
                ICommandHandler<LoginCommand, LoginResult> handler,
                CancellationToken ct)
            =>
            {
                var result = await handler.Handle(new(request), ct);
                return result.Success ? Results.Ok(result.Data) : Results.BadRequest(result);
            })
            .Produces<LoginResponse>()
            .Produces<LoginResult>(StatusCodes.Status400BadRequest);

        group.MapPut("/password", async (
                ChangePasswordRequest request,
                ICurrentUser currentUser,
                ICommandHandler<ChangePasswordCommand, AuthResult> handler,
                CancellationToken ct)
            =>
            {
                var result = await handler.Handle(new(currentUser.Id, request), ct);
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            })
            .Produces<AuthResult>()
            .Produces<AuthResult>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        group.MapGet("/me", async (
                ICurrentUser currentUser,
                IQueryHandler<GetCurrentUserQuery, UserDto?> handler,
                CancellationToken ct)
            => await handler.Handle(new(currentUser.Id), ct) is { } user
                ? Results.Ok(user)
                : Results.NotFound())
            .Produces<UserDto>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();
    }
}
