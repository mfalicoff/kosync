using Kosync.Auth;
using AuthenticationSchemes = Kosync.Auth.AuthenticationSchemes;

namespace Kosync.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKoreaderAuth(this IServiceCollection services)
    {
        services
            .AddAuthentication()
            .AddScheme<KoReaderAuthenticationSchemeOptions, KoReaderAuthenticationHandler>(
                AuthenticationSchemes.KoReaderScheme.KoReader,
                options =>
                {
                    options.AuthHeaderName = AuthenticationSchemes.KoReaderScheme.AuthHeaderName;
                    options.PasswordHeaderName = AuthenticationSchemes
                        .KoReaderScheme
                        .PasswordHeaderName;
                }
            );

        services
            .AddAuthorizationBuilder()
            .AddPolicy(
                AuthorizationPolicies.KoReaderAuth,
                policy =>
                    policy
                        .RequireClaim(ClaimTypes.Active, bool.TrueString.ToLowerInvariant())
                        .AddAuthenticationSchemes(AuthenticationSchemes.KoReaderScheme.KoReader)
                        .RequireAuthenticatedUser()
            )
            .AddPolicy(
                AuthorizationPolicies.AdminOnly,
                policy =>
                    policy
                        .RequireRole(ClaimTypes.IsAdmin)
                        .RequireClaim(ClaimTypes.Active, bool.TrueString.ToLowerInvariant())
                        .AddAuthenticationSchemes(AuthenticationSchemes.KoReaderScheme.KoReader)
            );

        return services;
    }
}
