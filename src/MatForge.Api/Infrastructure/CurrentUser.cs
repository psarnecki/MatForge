using System.Security.Claims;
using MatForge.Data;

namespace MatForge.Api.Infrastructure;

// Returns null when there is no request or nobody is logged in (startup seeding, dotnet ef, login)
public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? UserId =>
        Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : null;
}
