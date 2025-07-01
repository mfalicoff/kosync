using System.Threading.Tasks;
using Kosync.Database;
using Kosync.Extensions;
using Kosync.Models;
using Kosync.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Kosync.Endpoints;

public record UserAuthResponse(string Username);

public record CreateUserResponse(string Username);

public static class AuthEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapGroup("users").WithTags("Auth").MapAuthApi();
    }

    private static void MapAuthApi(this IEndpointRouteBuilder app)
    {
        app.MapGet("/auth", AuthoriseUser)
            .WithSummary("Authorise user")
            .WithDescription("Returns the username of the authenticated user.")
            .RequireAuthorization();

        app.MapPost("/create", CreateUser)
            .WithSummary("Create user")
            .WithDescription("Creates a new user account with the provided username and password.")
            .AllowAnonymous();
    }

    private static Results<Ok<UserAuthResponse>, ProblemHttpResult> AuthoriseUser(
        HttpContext context
    )
    {
        return TypedResults.Ok(new UserAuthResponse(context.User.Username()));
    }

    private static async Task<Results<Created<CreateUserResponse>, ProblemHttpResult>> CreateUser(
        HttpContext context,
        UserCreateRequest payload,
        IKosyncRepository kosyncRepository,
        IUserService userService
    )
    {
        await userService.CreateUserAsync(
            payload.username,
            payload.password,
            context.RequestAborted
        );

        return TypedResults.Created("", new CreateUserResponse(payload.username));
    }
}
