using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Persistence;
using Konbini.Api.Features.Auth.Models;
using Microsoft.EntityFrameworkCore;

namespace Konbini.Api.Features.Auth.Queries;

public record GetCurrentUserQuery(int UserId);

public class GetCurrentUserHandler(AppDbContext db)
    : IQueryHandler<GetCurrentUserQuery, UserDto?>
{
    public async Task<UserDto?> Handle(GetCurrentUserQuery query, CancellationToken ct)
        => await db.Users.AsNoTracking()
            .Where(u => u.Id == query.UserId)
            .Select(u => new UserDto(u.Id, u.Name, u.Email, u.Phone))
            .FirstOrDefaultAsync(ct);
}
