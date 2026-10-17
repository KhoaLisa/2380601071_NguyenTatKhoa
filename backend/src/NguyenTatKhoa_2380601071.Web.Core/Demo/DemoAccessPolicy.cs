using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Net;

namespace NguyenTatKhoa_2380601071.Demo
{
    public static class DemoAccessPolicy
    {
        public const string ClaimType = "toeicflow_demo";

        public static bool IsEnabled(IWebHostEnvironment environment, IConfiguration configuration)
        {
            return environment.IsDevelopment() && configuration.GetValue<bool>("DemoAccess:Enabled");
        }

        public static bool IsAllowedRequest(HttpContext context, IWebHostEnvironment environment, IConfiguration configuration)
        {
            if (!IsEnabled(environment, configuration)) return false;
            var remote = context.Connection.RemoteIpAddress;
            if (remote == null) return false;
            var allowedAddresses = configuration.GetSection("DemoAccess:AllowedRemoteAddresses").Get<string[]>() ?? Array.Empty<string>();
            if (!IPAddress.IsLoopback(remote) && !allowedAddresses.Any(value =>
                IPAddress.TryParse(value, out var allowed) && allowed.MapToIPv6().Equals(remote.MapToIPv6()))) return false;

            var origin = context.Request.Headers["Origin"].ToString();
            if (string.IsNullOrEmpty(origin)) return true; // Native clients have no browser Origin.
            var requestOrigin = context.Request.Scheme + "://" + context.Request.Host;
            if (string.Equals(origin, requestOrigin, StringComparison.OrdinalIgnoreCase)) return true;
            return (configuration["App:CorsOrigins"] ?? string.Empty).Split(',')
                .Any(value => string.Equals(value.Trim().TrimEnd('/'), origin.TrimEnd('/'), StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsLecturerPage(PathString path)
        {
            return path == "/" || path.StartsWithSegments("/Home") || path.StartsWithSegments("/Exams");
        }

        public static bool IsAllowedDemoPath(PathString path, string kind)
        {
            if (path.StartsWithSegments("/api/toeic/content") || path.StartsWithSegments("/api/demo/student-session")) return true;
            if (path.StartsWithSegments("/api/services/app/StudentExam")) return kind == "student";
            if (kind != "lecturer") return false;
            return IsLecturerPage(path) || path.StartsWithSegments("/api/services/app/ExamManagement");
        }
    }
}
