using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Kosync.Database.Entities;
using Kosync.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kosync.Auth;

public class KoReaderAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public string AuthHeaderName { get; set; } = "x-auth-user";
    public string PasswordHeaderName { get; set; } = "x-auth-key";
}

public class KoReaderAuthenticationHandler(
    IOptionsMonitor<KoReaderAuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IUserService userService
) : AuthenticationHandler<KoReaderAuthenticationSchemeOptions>(options, logger, encoder)
{
    private readonly IUserService _userService = userService;

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (
            !Request.Headers.ContainsKey(Options.AuthHeaderName)
            || !Request.Headers.ContainsKey(Options.PasswordHeaderName)
        )
        {
            return AuthenticateResult.Fail("Missing authentication headers");
        }

        string? username = Request.Headers[Options.AuthHeaderName].FirstOrDefault();
        string? password = Request.Headers[Options.PasswordHeaderName].FirstOrDefault();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return AuthenticateResult.Fail("Invalid authentication headers");
        }

        UserDocument? user = await _userService.GetUserByUsernameAsync(
            username,
            Request.HttpContext.RequestAborted
        );

        if (user == null || user.PasswordHash != password)
        {
            return AuthenticateResult.Fail("Invalid credentials");
        }

        List<Claim> claims =
        [
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Active, user.IsActive.ToString()),
            new(ClaimTypes.UserType, user.IsAdministrator ? ClaimTypes.Admin : ClaimTypes.User)
        ];

        ClaimsIdentity identity = new(claims, Scheme.Name);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
