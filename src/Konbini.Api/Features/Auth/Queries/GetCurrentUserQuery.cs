using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Auth.Repositories;
using Konbini.Api.Features.Common.Abstractions;

namespace Konbini.Api.Features.Auth.Queries;

public record GetCurrentUserQuery(int UserId);

public class GetCurrentUserHandler(IUserRepository users)
    : IQueryHandler<GetCurrentUserQuery, UserDto?>
{
    public async Task<UserDto?> Handle(GetCurrentUserQuery query, CancellationToken ct)
        => await users.GetProfileAsync(query.UserId, ct);
}
