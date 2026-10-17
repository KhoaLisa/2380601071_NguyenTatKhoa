using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using NguyenTatKhoa_2380601071.Configuration;

namespace NguyenTatKhoa_2380601071.Demo
{
    public static class DemoAccessExtensions
    {
        public static IApplicationBuilder UseToeicDemoAccess(this IApplicationBuilder app, bool lecturerWebsite)
        {
            return app.Use(async (context, next) =>
            {
                var environment = (IWebHostEnvironment)context.RequestServices.GetService(typeof(IWebHostEnvironment));
                var configuration = environment.GetAppConfiguration();
                var kind = context.User.FindFirstValue(DemoAccessPolicy.ClaimType);
                if (kind != null && (!DemoAccessPolicy.IsAllowedRequest(context, environment, configuration) ||
                    !DemoAccessPolicy.IsAllowedDemoPath(context.Request.Path, kind)))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }

                if (lecturerWebsite && DemoAccessPolicy.IsEnabled(environment, configuration) &&
                    context.Request.Path.StartsWithSegments("/Account"))
                {
                    if (!DemoAccessPolicy.IsAllowedRequest(context, environment, configuration))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return;
                    }
                    context.Response.Redirect("/");
                    return;
                }

                if (lecturerWebsite && context.User.Identity?.IsAuthenticated != true &&
                    DemoAccessPolicy.IsLecturerPage(context.Request.Path) && DemoAccessPolicy.IsAllowedRequest(context, environment, configuration))
                {
                    var identities = (DemoIdentityService)context.RequestServices.GetService(typeof(DemoIdentityService));
                    context.User = await identities.GetLecturerAsync();
                    await context.SignInAsync(IdentityConstants.ApplicationScheme, context.User, new AuthenticationProperties
                    {
                        IsPersistent = false,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                    });
                }
                await next();
            });
        }
    }
}
