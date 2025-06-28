using System;
using System.Threading.Tasks;
using Kosync.Database;
using Kosync.Database.Entities;
using Kosync.Extensions;
using Kosync.Models;
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

public static class SyncEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGroup("/v2/syncs").WithTags("SyncV2").RequireAuthorization().MapSyncApi();
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
            .WithDescription(
                "Retrieves the current reading progress and metadata for a specific document"
            );
    }

    private static async Task<
        Results<Ok<DocumentUpdateResponse>, BadRequest<string>, ProblemHttpResult>
    > UpdateDocumentProgress(
        HttpRequest request,
        DocumentRequest payload,
        IKosyncRepository repository
    )
    {
        try
        {
            DocumentProgress document =
                await repository.GetDocumentAsync(
                    request.HttpContext.User.Username(),
                    payload.document
                ) ?? new DocumentProgress { DocumentHash = payload.document };

            document.Progress = payload.progress;
            document.Percentage = payload.percentage;
            document.Device = payload.device;
            document.DeviceId = payload.device_id;
            document.Timestamp = DateTime.UtcNow;

            await repository.AddOrUpdateDocumentAsync(
                request.HttpContext.User.Username(),
                document
            );

            return TypedResults.Ok(
                new DocumentUpdateResponse(document.DocumentHash, document.Timestamp)
            );
        }
        catch (Exception)
        {
            return TypedResults.Problem("Unable to update document progress");
        }
    }

    private static async Task<
        Results<Ok<DocumentProgressRequest>, NotFound<string>, ProblemHttpResult>
    > GetDocumentProgress(HttpRequest request, string documentHash, IKosyncRepository repository)
    {
        try
        {
            DocumentProgress? document = await repository.GetDocumentAsync(
                request.HttpContext.User.Username(),
                documentHash
            );

            if (document is null)
            {
                return TypedResults.NotFound("Document not found on server");
            }

            DateTimeOffset time = new(document.Timestamp);

            DocumentProgressRequest result = new(
                document.Device,
                document.DeviceId,
                document.DocumentHash,
                document.Percentage,
                document.Progress,
                time.ToUnixTimeSeconds()
            );

            return TypedResults.Ok(result);
        }
        catch (Exception)
        {
            return TypedResults.Problem("Unable to retrieve document progress");
        }
    }
}
