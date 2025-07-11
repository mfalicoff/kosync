using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kosync.Auth;
using Kosync.Database;
using Kosync.Database.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Kosync.Endpoints.Management;

public record DocumentResponse(
    string DocumentHash,
    string Progress,
    decimal Percentage,
    string Device,
    string DeviceId,
    DateTime Timestamp
);

public static class DocumentManagementEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder documentGroup = app.MapGroup("/manage/users/documents")
            .WithTags("Document Management")
            .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        documentGroup
            .MapGet("/", GetUserDocuments)
            .WithName("GetUserDocuments")
            .WithSummary("Get all documents for a user");

        documentGroup
            .MapDelete("/", DeleteUserDocument)
            .WithName("DeleteUserDocument")
            .WithSummary("Delete a specific user document");
    }

    private static async Task<
        Results<Ok<IEnumerable<DocumentResponse>>, BadRequest<string>, ProblemHttpResult>
    > GetUserDocuments(HttpContext context, string username, IKosyncRepository kosyncRepository)
    {
        try
        {
            UserDocument? user = await kosyncRepository.GetUserByUsernameAsync(username);
            if (user is null)
            {
                return TypedResults.BadRequest("User does not exist");
            }

            IEnumerable<DocumentResponse> documents = user.Documents.Values.Select(doc => new DocumentResponse(
                doc.DocumentHash,
                doc.Progress,
                doc.Percentage,
                doc.Device,
                doc.DeviceId,
                doc.Timestamp
            ));

            return TypedResults.Ok(documents);
        }
        catch (Exception ex)
        {
            return TypedResults.Problem(detail: "An error occurred while retrieving user documents", statusCode: 500);
        }
    }

    private static async Task<Results<Ok, BadRequest<string>, NotFound<string>, ProblemHttpResult>> DeleteUserDocument(
        HttpContext context,
        string username,
        string documentHash,
        IKosyncRepository kosyncRepository
    )
    {
        try
        {
            UserDocument? user = await kosyncRepository.GetUserByUsernameAsync(username);
            if (user is null)
            {
                return TypedResults.BadRequest("User does not exist");
            }

            if (!user.Documents.ContainsKey(documentHash))
            {
                return TypedResults.NotFound("Document does not exist for this user");
            }

            bool success = await kosyncRepository.RemoveDocumentAsync(username, documentHash);
            if (!success)
            {
                return TypedResults.Problem(detail: "Failed to delete document", statusCode: 500);
            }

            return TypedResults.Ok();
        }
        catch (Exception ex)
        {
            return TypedResults.Problem(detail: "An error occurred while deleting the document", statusCode: 500);
        }
    }
}
