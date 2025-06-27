using Kosync.Auth;
using Kosync.Database;
using Kosync.Database.Entities;
using Kosync.Services;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
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
    
    public static IServiceCollection AddMongoDb(this IServiceCollection services, MongoDbOptions options)
    {
        services.AddSingleton<IMongoClient>(_ => new MongoClient(options.ConnectionString));

        services.AddSingleton<IMongoDatabase>(serviceProvider =>
        {
            IMongoClient client = serviceProvider.GetRequiredService<IMongoClient>();
            return client.GetDatabase(options.DatabaseName);
        });

        services.AddSingleton<IMongoCollection<UserDocument>>(serviceProvider =>
        {
            IMongoDatabase database = serviceProvider.GetRequiredService<IMongoDatabase>();
            return database.GetCollection<UserDocument>(options.CollectionName);
        });
        
        services.AddHostedService<MongoInitializerService>();
        services.AddTransient<IKosyncRepository, KosyncRepository>();
        return services;
    }
}
