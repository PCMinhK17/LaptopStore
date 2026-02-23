using System.Security.Claims;

namespace LaptopStore.Extensions;

public static class Identity
{
    public static int? GetUserId(this ClaimsPrincipal user)
    {
        if (user.Identity == null || !user.Identity.IsAuthenticated)
        {
            return null;
        }

        string? userIdString = user.FindFirstValue("UserId");
        if (!int.TryParse(userIdString, out int userId))
        {
            return null;
        }

        return userId;
    }
}
