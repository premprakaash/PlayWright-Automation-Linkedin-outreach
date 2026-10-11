using System.Security.Claims;

namespace CodeInspector.Leads.Api.Auth;

public interface IUserContext
{
    Guid UserId { get; }
    bool IsAdmin { get; }
}

public class UserContext(IHttpContextAccessor accessor) : IUserContext
{
    public Guid UserId
    {
        get
        {
            var sub = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? accessor.HttpContext?.User.FindFirstValue("sub");
            if (Guid.TryParse(sub, out var id))
            {
                return id;
            }

            throw new UnauthorizedAccessException("User is not authenticated.");
        }
    }

    public bool IsAdmin =>
        accessor.HttpContext?.User.IsInRole("Admin") == true ||
        accessor.HttpContext?.User.HasClaim("role", "admin") == true;
}
