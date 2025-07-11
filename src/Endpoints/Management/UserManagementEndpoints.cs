using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kosync.Auth;
using Kosync.Database.Entities;
using Kosync.Extensions;
using Kosync.Models;
using Kosync.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Kosync.Endpoints.Management;

public record UserRestrictedResponse(
    string? Id,
    string Username,
    bool IsActive,
    bool IsAdministrator,
    int DocumentCount
);

public static class UserManagementEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder userGroup = app.MapGroup("/manage/users")
            .WithTags("User Management")
            .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        // User collection operations
        userGroup.MapGet("/", GetUsers).WithName("GetAllUsers").WithSummary("Get all users");

        userGroup.MapPost("/", CreateUser).WithName("CreateUser").WithSummary("Create a new user");

        userGroup.MapDelete("/", DeleteUser).WithName("DeleteUser").WithSummary("Delete a user");

        userGroup
            .MapPut("/active", SetUserActiveStatus)
            .WithName("SetUserActiveStatus")
            .WithSummary("Activate or deactivate a user");

        userGroup.MapPut("/password", SetUserPassword).WithName("SetUserPassword").WithSummary("Update user password");
    }

    private static async Task<Results<Ok<IEnumerable<UserRestrictedResponse>>, ProblemHttpResult>> GetUsers(
        HttpContext context,
        IUserService userService
    )
    {
        IEnumerable<UserDocument> users = await userService.GetUserDocumentsAsync(context.RequestAborted);

        IEnumerable<UserRestrictedResponse> userList = users.Select(u => new UserRestrictedResponse(
            u.Id,
            u.Username,
            u.IsActive,
            u.IsAdministrator,
            u.Documents.Count
        ));

        return TypedResults.Ok(userList);
    }

    private static async Task<Results<Created, ProblemHttpResult>> CreateUser(
        HttpContext context,
        UserCreateRequest payload,
        IUserService userService
    )
    {
        string passwordHash = payload.password.HashPassword();
        await userService.CreateUserAsync(payload.username, passwordHash, context.RequestAborted);

        return TypedResults.Created("");
    }

    private static async Task<Results<NoContent, BadRequest, ProblemHttpResult>> DeleteUser(
        HttpContext context,
        string username,
        IUserService userService
    )
    {
        await userService.DeleteUserAsync(username, context.RequestAborted);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok, BadRequest, ProblemHttpResult>> SetUserActiveStatus(
        HttpContext context,
        string username,
        bool isActive,
        IUserService userService
    )
    {
        // add admin check
        await userService.UpdateUserStatusAsync(username, isActive, context.RequestAborted);
        return TypedResults.Ok();
    }

    private static async Task<Results<Ok, BadRequest, ProblemHttpResult>> SetUserPassword(
        HttpContext context,
        string username,
        PasswordChangeRequest payload,
        IUserService userService
    )
    {
        // admin check
        await userService.UpdateUserPasswordAsync(username, payload.password, context.RequestAborted);
        return TypedResults.Ok();
    }
}
