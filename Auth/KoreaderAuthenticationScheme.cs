using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Kosync.Database;
using Kosync.Database.Entities;
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
    IKosyncRepository kosyncRepository)
    : AuthenticationHandler<KoReaderAuthenticationSchemeOptions>(options, logger, encoder)
{
    private readonly IKosyncRepository _kosyncRepository = kosyncRepository;
    
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey(Options.AuthHeaderName) || 
            !Request.Headers.ContainsKey(Options.PasswordHeaderName))
        {
            return AuthenticateResult.Fail("Missing authentication headers");
        }

        var username = Request.Headers[Options.AuthHeaderName].FirstOrDefault();
        var password = Request.Headers[Options.PasswordHeaderName].FirstOrDefault();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return AuthenticateResult.Fail("Invalid authentication headers");
        }
        
        UserDocument? user = await _kosyncRepository.GetUserByUsernameAsync(username);
        
        if (user == null || user.PasswordHash != password)
        {
            return AuthenticateResult.Fail("Invalid credentials");
        }

        List<Claim> claims =
        [
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Active, user.IsActive.ToString()),
            new(ClaimTypes.IsAdmin, user.IsAdministrator.ToString()),

        ];
        
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}