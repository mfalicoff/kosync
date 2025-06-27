using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kosync.Database.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
namespace Kosync.Services;

public class MongoDbOptions
{
    public static string SectionName = "MongoDB";

    public required string ConnectionString { get; init; }
    public required string DatabaseName { get; init; }
    public required string AdminPassword { get; init; }
    public required string CollectionName { get; init; }
}

public class MongoInitializerService(IOptions<MongoDbOptions> options, IMongoDatabase database) : IHostedService
{
    private readonly MongoDbOptions _mongoDbOptions = options.Value;
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await InitializeDatabaseAsync(database, cancellationToken);
        await CreateIndexesAsync(database, cancellationToken);
    }

    private async Task InitializeDatabaseAsync(IMongoDatabase database, CancellationToken cancellationToken)
    {
        IAsyncCursor<string>? collections = await database.ListCollectionNamesAsync(cancellationToken: cancellationToken);
        List<string>? collectionList = await collections.ToListAsync(cancellationToken);

        if (!collectionList.Contains(_mongoDbOptions.CollectionName))
        {
            await database.CreateCollectionAsync(_mongoDbOptions.CollectionName, cancellationToken: cancellationToken);
        }
    }
    
    private async Task CreateIndexesAsync(IMongoDatabase database, CancellationToken cancellationToken)
    {
        IMongoCollection<UserDocument>? usersCollection = database.GetCollection<UserDocument>(_mongoDbOptions.CollectionName);
            
        // Create unique index on username
        IndexKeysDefinition<UserDocument>? usernameIndexKeys = Builders<UserDocument>.IndexKeys.Ascending(user => user.Username);
        CreateIndexModel<UserDocument> usernameIndexModel = new(
            usernameIndexKeys, 
            new CreateIndexOptions { Unique = true, Name = "username_unique" });
            
        // Create compound index for document queries
        IndexKeysDefinition<UserDocument>? documentIndexKeys = Builders<UserDocument>.IndexKeys
            .Ascending(user => user.Username)
            .Ascending("documents");
        CreateIndexModel<UserDocument> documentIndexModel = new(
            documentIndexKeys, 
            new CreateIndexOptions { Name = "username_documents" });
            
        // Create index for active users
        IndexKeysDefinition<UserDocument>? activeIndexKeys = Builders<UserDocument>.IndexKeys.Ascending(user => user.IsActive);
        CreateIndexModel<UserDocument> activeIndexModel = new(
            activeIndexKeys, 
            new CreateIndexOptions { Name = "is_active" });
            
        await usersCollection.Indexes.CreateManyAsync(
            [usernameIndexModel, documentIndexModel, activeIndexModel], 
            cancellationToken);
            
    }
    
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
