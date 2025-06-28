using Kosync.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Kosync.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseIpDetection(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<IpDetectionMiddleware>();
    }
}
