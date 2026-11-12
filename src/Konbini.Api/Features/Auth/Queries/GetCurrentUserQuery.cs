using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Auth.Repositories;
using Konbini.Api.Features.Common.Abstractions;

namespace Konbini.Api.Features.Auth.Queries;

public record GetCurrentUserQuery(int UserId);

public class GetCurrentUserHandler(IUserRepository users)
    : IQueryHandler<GetCurrentUserQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(GetCurrentUserQuery query, CancellationToken ct)
    {
        // token 有效但帳號已不存在：前端收到 404 會登出
        if (await users.GetProfileAsync(query.UserId, ct) is not { } user)
        {
            return Error.NotFound("找不到使用者");
        }
        return user;
    }
}
