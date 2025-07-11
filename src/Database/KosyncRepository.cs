using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Kosync.Database.Entities;

public class KosyncRepository(IMongoCollection<UserDocument> collection, ILogger<KosyncRepository> logger)
    : IKosyncRepository
{
    private readonly ILogger<KosyncRepository> _logger = logger;
    private readonly IMongoCollection<UserDocument> _users = collection;

    // User operations
    public async Task<UserDocument?> GetUserByUsernameAsync(string username)
    {
        return await _users.Find(u => u.Username == username).FirstOrDefaultAsync();
    }

    public async Task<UserDocument?> GetUserByIdAsync(string id)
    {
        return await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<UserDocument>> GetAllUsersAsync()
    {
        return await _users.Find(_ => true).ToListAsync();
    }

    public async Task<bool> CreateUserAsync(UserDocument user)
    {
        try
        {
            await _users.InsertOneAsync(user);
            _logger.LogInformation("Created user: {Username}", user.Username);
            return true;
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            _logger.LogWarning("Attempted to create duplicate user: {Username}", user.Username);
            return false;
        }
    }

    public async Task<bool> UpdateUserAsync(UserDocument user)
    {
        ReplaceOneResult? result = await _users.ReplaceOneAsync(u => u.Id == user.Id, user);
        if (result.ModifiedCount > 0)
        {
            _logger.LogInformation("Updated user: {Username}", user.Username);
        }
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteUserAsync(string username)
    {
        DeleteResult? result = await _users.DeleteOneAsync(u => u.Username == username);
        if (result.DeletedCount > 0)
        {
            _logger.LogInformation("Deleted user with ID: {Username}", username);
        }
        return result.DeletedCount > 0;
    }

    // Document operations
    public async Task<bool> AddOrUpdateDocumentAsync(string username, DocumentProgress document)
    {
        FilterDefinition<UserDocument>? filter = Builders<UserDocument>.Filter.Eq(u => u.Username, username);
        UpdateDefinition<UserDocument>? update = Builders<UserDocument>.Update.Set(
            $"documents.{document.DocumentHash}",
            document
        );

        UpdateResult? result = await _users.UpdateOneAsync(filter, update);
        if (result.ModifiedCount > 0)
        {
            _logger.LogDebug("Updated document {DocumentHash} for user {Username}", document.DocumentHash, username);
        }
        return result.ModifiedCount > 0;
    }

    public async Task<DocumentProgress?> GetDocumentAsync(string username, string documentHash)
    {
        UserDocument? user = await GetUserByUsernameAsync(username);
        return user?.GetDocument(documentHash);
    }

    public async Task<bool> RemoveDocumentAsync(string username, string documentHash)
    {
        FilterDefinition<UserDocument>? filter = Builders<UserDocument>.Filter.Eq(u => u.Username, username);
        UpdateDefinition<UserDocument>? update = Builders<UserDocument>.Update.Unset($"documents.{documentHash}");

        UpdateResult? result = await _users.UpdateOneAsync(filter, update);
        if (result.ModifiedCount > 0)
        {
            _logger.LogDebug("Removed document {DocumentHash} for user {Username}", documentHash, username);
        }
        return result.ModifiedCount > 0;
    }

    public async Task<IEnumerable<DocumentProgress>> GetUserDocumentsAsync(string username)
    {
        UserDocument? user = await GetUserByUsernameAsync(username);
        return user?.GetAllDocuments() ?? new List<DocumentProgress>();
    }

    public async Task<bool> UpdateMultipleDocumentsAsync(
        string username,
        Dictionary<string, DocumentProgress> documents
    )
    {
        FilterDefinition<UserDocument>? filter = Builders<UserDocument>.Filter.Eq(u => u.Username, username);
        UpdateDefinitionBuilder<UserDocument>? updateBuilder = Builders<UserDocument>.Update;
        List<UpdateDefinition<UserDocument>> updates = [];

        foreach (KeyValuePair<string, DocumentProgress> kvp in documents)
        {
            updates.Add(updateBuilder.Set($"documents.{kvp.Key}", kvp.Value));
        }

        UpdateDefinition<UserDocument>? combinedUpdate = updateBuilder.Combine(updates);
        UpdateResult? result = await _users.UpdateOneAsync(filter, combinedUpdate);

        if (result.ModifiedCount > 0)
        {
            _logger.LogDebug("Updated {DocumentCount} documents for user {Username}", documents.Count, username);
        }

        return result.ModifiedCount > 0;
    }

    // Advanced queries
    public async Task<IEnumerable<UserDocument>> GetUsersWithDocumentAsync(string documentHash)
    {
        FilterDefinition<UserDocument>? filter = Builders<UserDocument>.Filter.Exists($"documents.{documentHash}");
        return await _users.Find(filter).ToListAsync();
    }

    public async Task<long> GetTotalDocumentCountAsync()
    {
        BsonDocument[] pipeline =
        [
            BsonDocument.Parse("{ $project: { documentCount: { $size: { $objectToArray: '$documents' } } } }"),
            BsonDocument.Parse("{ $group: { _id: null, totalDocuments: { $sum: '$documentCount' } } }"),
        ];

        BsonDocument? result = await _users.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync();
        return result?["totalDocuments"].AsInt64 ?? 0;
    }
}
