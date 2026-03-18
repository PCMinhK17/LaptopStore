using LaptopStore.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LaptopStore.Middleware
{
    public class CheckBannedUserMiddleware
    {
        private readonly RequestDelegate _next;

        public CheckBannedUserMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, LaptopStoreDbContext dbContext)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier) ?? context.User.FindFirst("UserId");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    // Tránh kiểm tra liên tục trên database có thể gây chậm, nhưng để đảm bảo force logout ngay lập tức ta nên check
                    // Hoặc có thể dùng Cache ở đây
                    var userStatus = await dbContext.Users
                        .Where(u => u.Id == userId)
                        .Select(u => u.Status)
                        .FirstOrDefaultAsync();

                    if (userStatus == "locked")
                    {
                        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        context.Session.Clear();
                        
                        // Chuyển hướng về login với thông báo
                        var returnUrl = context.Request.Path + context.Request.QueryString;
                        context.Response.Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }

    public static class CheckBannedUserMiddlewareExtensions
    {
        public static IApplicationBuilder UseCheckBannedUser(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CheckBannedUserMiddleware>();
        }
    }
}
