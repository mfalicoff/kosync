using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kosync.Middleware;

public class IpDetectionMiddleware(
    RequestDelegate next,
    ILogger<IpDetectionMiddleware> logger,
    IOptions<IpDetectionOptions> options
)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<IpDetectionMiddleware> _logger = logger;
    private readonly IpDetectionOptions _options = options.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        IpInfo ipInfo = DetectClientIp(context);

        context.Items["ClientIP"] = ipInfo.ClientIp;
        context.Items["TrustedProxy"] = ipInfo.TrustedProxy;
        context.Items["ConnectingIP"] = ipInfo.ConnectingIp;

        await _next(context);
    }

    private IpInfo DetectClientIp(HttpContext context)
    {
        string connectingIp = GetConnectingIp(context);
        bool isTrustedProxy = _options.TrustedProxies.Contains(connectingIp);
        string clientIp = connectingIp;

        if (isTrustedProxy)
        {
            string? forwardedIp = GetForwardedIp(context);
            if (!string.IsNullOrEmpty(forwardedIp))
            {
                clientIp = forwardedIp;
            }
            else
            {
                LogWarning($"Trusted proxy [{connectingIp}] failed to forward client IP address.");
            }
        }

        if (string.IsNullOrEmpty(clientIp))
        {
            LogWarning("Unable to determine client IP address.");
            clientIp = "";
        }

        return new IpInfo
        {
            ClientIp = clientIp,
            TrustedProxy = isTrustedProxy,
            ConnectingIp = connectingIp,
        };
    }

    private string GetConnectingIp(HttpContext context)
    {
        IPAddress? remoteIp = context.Connection.RemoteIpAddress;

        if (remoteIp is null)
        {
            return "";
        }

        if (remoteIp.IsIPv4MappedToIPv6)
        {
            remoteIp = remoteIp.MapToIPv4();
        }

        return remoteIp.ToString();
    }

    private string? GetForwardedIp(HttpContext context)
    {
        string? forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();

        if (string.IsNullOrEmpty(forwardedFor))
        {
            return null;
        }

        // Take the first IP from the comma-separated list
        forwardedFor = forwardedFor
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        if (!string.IsNullOrEmpty(forwardedFor) && IPAddress.TryParse(forwardedFor, out _))
        {
            return forwardedFor;
        }

        return null;
    }

    private void LogWarning(string text)
    {
        Log(LogLevel.Warning, text);
    }

    private void LogInfo(string text)
    {
        Log(LogLevel.Information, text);
    }

    private void Log(LogLevel level, string text)
    {
        string message = $"[{DateTime.Now:yyyy/MM/dd HH:mm:ss}] {text}";
        _logger.Log(level, message);
    }
}

public class IpInfo
{
    public string ClientIp { get; set; } = "";
    public bool TrustedProxy { get; set; }
    public string ConnectingIp { get; set; } = "";
}

public class IpDetectionOptions
{
    public string[] TrustedProxies { get; set; } = [];
}
