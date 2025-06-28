using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Kosync.Endpoints;

public static class HealthCheckEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGroup("/v2").WithTags("HealthChecks").MapHealthCheck();
    }

    private static void MapHealthCheck(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", () => Results.Ok("kosync-dotnet server is running."))
            .WithSummary("Server Status")
            .WithDescription("Returns the status of the kosync-dotnet server.");

        app.MapGet("/healthcheck", () => Results.Ok(new { state = "OK" }))
            .WithSummary("Health Check")
            .WithDescription("Checks the health status of the server.");
    }
}
