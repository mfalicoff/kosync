using System;
using System.Text.Json;
using System.Threading.Tasks;
using Kosync.Database.Entities;
using Kosync.Extensions;
using Kosync.Models;
using Kosync.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Kosync.Endpoints;

public record DocumentUpdateResponse(string Document, DateTime Timestamp);

public record DocumentProgressRequest(
    string Device,
    string DeviceId,
    string Document,
    decimal Percentage,
    string Progress,
    long Timestamp
);

public record DocumentProgressResponse(
    string device,
    string deviceId,
    string document,
    decimal percentage,
    string progress,
    long timestamp
);

public static class SyncEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGroup("syncs").WithTags("Sync").RequireAuthorization().MapSyncApi();
    }

    private static void MapSyncApi(this IEndpointRouteBuilder app)
    {
        app.MapPut("/progress", UpdateDocumentProgress)
            .WithSummary("Update document reading progress")
            .WithDescription(
                "Updates the reading progress, percentage, and device information for a specific document"
            );

        app.MapGet("/progress/{documentHash}", GetDocumentProgress)
            .WithSummary("Get document reading progress")
            .WithDescription("Retrieves the current reading progress and metadata for a specific document");
    }

    private static async Task<
        Results<Ok<DocumentUpdateResponse>, BadRequest<string>, ProblemHttpResult>
    > UpdateDocumentProgress(HttpRequest request, DocumentRequest payload, ISyncService syncService)
    {
        DocumentProgress? result = await syncService.UpdateProgressAsync(
            request.HttpContext.User.Username(),
            payload.document,
            payload,
            request.HttpContext.RequestAborted
        );

        if (result is null)
        {
            return TypedResults.BadRequest("Unable to update document progress");
        }

        return TypedResults.Ok(new DocumentUpdateResponse(result.DocumentHash, result.Timestamp));
    }

    private static async Task<IResult> GetDocumentProgress(
        HttpRequest request,
        string documentHash,
        ISyncService syncService
    )
    {
        DocumentProgress? result = await syncService.GetDocumentProgressAsync(
            request.HttpContext.User.Username(),
            documentHash,
            request.HttpContext.RequestAborted
        );

        if (result is null)
        {
            return TypedResults.NotFound($"No progress found for document {documentHash}");
        }

        DateTimeOffset time = new(result.Timestamp);

        DocumentProgressResponse response = new(
            result.Device,
            result.DeviceId,
            result.DocumentHash,
            result.Percentage,
            result.Progress,
            time.ToUnixTimeSeconds()
        );

        string json = JsonSerializer.Serialize(response);
        return Results.Content(json, "application/json", null, 200);
    }
}
