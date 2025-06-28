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

public record UserAuthResponse(string Username);

public record CreateUserResponse(string Username);

public static class AuthEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapGroup("/v2/users").WithTags("Auth").MapAuthApi();
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
        UserCreateRequest payload,
        IKosyncRepository kosyncRepository
    )
    {
        if (Environment.GetEnvironmentVariable("REGISTRATION_DISABLED") == "true")
        {
            return TypedResults.Problem("User registration is disabled", statusCode: 402);
        }

        UserDocument? existing = await kosyncRepository.GetUserByUsernameAsync(payload.username);

        if (existing is not null)
        {
            return TypedResults.Problem("User already exists", statusCode: 402);
        }

        await kosyncRepository.CreateUserAsync(
            new UserDocument
            {
                Username = payload.username,
                PasswordHash = payload.password,
                IsAdministrator = false,
                IsActive = true,
            }
        );

        return TypedResults.Created("", new CreateUserResponse(payload.username));
    }
}
