using Microsoft.AspNetCore.Http;

namespace Kosync.Extensions;

public static class HttpContextExtensions
{
    public static string GetClientIP(this HttpContext context) =>
        context.Items["ClientIP"]?.ToString() ?? "";

    public static bool IsTrustedProxy(this HttpContext context) =>
        context.Items["TrustedProxy"] as bool? ?? false;

    public static string GetConnectingIP(this HttpContext context) =>
        context.Items["ConnectingIP"] as string ?? "";
}
