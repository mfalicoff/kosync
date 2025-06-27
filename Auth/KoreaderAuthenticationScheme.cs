using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
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
    KosyncDb db)
    : AuthenticationHandler<KoReaderAuthenticationSchemeOptions>(options, logger, encoder)
{
    private readonly KosyncDb _db = db;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey(Options.AuthHeaderName) || 
            !Request.Headers.ContainsKey(Options.PasswordHeaderName))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing authentication headers"));
        }

        var username = Request.Headers[Options.AuthHeaderName].FirstOrDefault();
        var password = Request.Headers[Options.PasswordHeaderName].FirstOrDefault();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid authentication headers"));
        }

        ILiteCollection<User>? userCollection = _db.Context.GetCollection<User>("users");
        User? user = userCollection.FindOne(i => i.Username == username && i.PasswordHash == password);
        
        if (user == null)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid credentials"));
        }

        List<Claim> claims =
        [
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Id, user.Id.ToString()),
            new(ClaimTypes.Active, user.IsActive.ToString()),
            new(ClaimTypes.IsAdmin, user.IsAdministrator.ToString()),

        ];
        
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}