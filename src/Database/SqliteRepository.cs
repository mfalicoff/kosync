using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Kosync.Database.Entities;
using System.Text.Json;
using System.Linq;
using System;
using SqlKata.Execution;

namespace Kosync.Database;

public static class Tables
{
    public const string Users = "Users";
}

public static class UserColumns
{
    public const string Id = "Id";
    public const string Username = "Username";
    public const string PasswordHash = "PasswordHash";
    public const string IsActive = "IsActive";
    public const string IsAdministrator = "IsAdministrator";
    public const string Documents = "Documents";
}

public class SqliteRepository(SqliteConnection connection, ILogger<SqliteRepository> logger) : IKosyncRepository
{
    private readonly ILogger<SqliteRepository> _logger = logger;
    
    private readonly QueryFactory _db = new(connection, new SqlKata.Compilers.SqliteCompiler());

    // User operations
    public async Task<UserDocument?> GetUserByUsernameAsync(string username)
    {
        UserRow? result = await _db.Query(Tables.Users)
            .Where(UserColumns.Username, username)
            .FirstOrDefaultAsync<UserRow>();
        return result?.ToUserDocument();
    }

    public async Task<UserDocument?> GetUserByIdAsync(string id)
    {
        UserRow? result = await _db.Query(Tables.Users)
            .Where(UserColumns.Id, id)
            .FirstOrDefaultAsync<UserRow>();
        return result?.ToUserDocument();
    }

    public async Task<IEnumerable<UserDocument>> GetAllUsersAsync()
    {
        IEnumerable<UserRow> results = await _db.Query(Tables.Users)
            .GetAsync<UserRow>();
        return results.Select(r => r.ToUserDocument());
    }

    public async Task<bool> CreateUserAsync(UserDocument user)
    {
        try
        {
            UserRow userRow = UserRow.FromUserDocument(user);
            int result = await _db.Query(Tables.Users)
                .InsertAsync(userRow);
            
            if (result > 0)
            {
                _logger.LogInformation("Created user: {Username}", user.Username);
                return true;
            }
            return false;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // SQLITE_CONSTRAINT
        {
            _logger.LogWarning("Attempted to create duplicate user: {Username}", user.Username);
            return false;
        }
    }

    public async Task<bool> UpdateUserAsync(UserDocument user)
    {
        UserRow userRow = UserRow.FromUserDocument(user);
        int result = await _db.Query(Tables.Users)
            .Where(UserColumns.Id, userRow.Id)
            .UpdateAsync(new
            {
                Username = userRow.Username,
                PasswordHash = userRow.PasswordHash,
                IsActive = userRow.IsActive,
                IsAdministrator = userRow.IsAdministrator,
                Documents = userRow.Documents
            });
        
        if (result > 0)
        {
            _logger.LogInformation("Updated user: {Username}", user.Username);
        }
        return result > 0;
    }

    public async Task<bool> DeleteUserAsync(string username)
    {
        int result = await _db.Query(Tables.Users)
            .Where(UserColumns.Username, username)
            .DeleteAsync();
        
        if (result > 0)
        {
            _logger.LogInformation("Deleted user with ID: {Username}", username);
        }
        return result > 0;
    }

    // Document operations
    public async Task<bool> AddOrUpdateDocumentAsync(string username, DocumentProgress document)
    {
        UserDocument? user = await GetUserByUsernameAsync(username);
        if (user == null) return false;

        user.AddOrUpdateDocument(document);
        return await UpdateUserAsync(user);
    }

    public async Task<DocumentProgress?> GetDocumentAsync(string username, string documentHash)
    {
        UserDocument? user = await GetUserByUsernameAsync(username);
        return user?.GetDocument(documentHash);
    }

    public async Task<bool> RemoveDocumentAsync(string username, string documentHash)
    {
        UserDocument? user = await GetUserByUsernameAsync(username);
        if (user == null) return false;

        bool removed = user.RemoveDocument(documentHash);
        if (removed)
        {
            await UpdateUserAsync(user);
            _logger.LogDebug("Removed document {DocumentHash} for user {Username}", documentHash, username);
        }
        return removed;
    }

    public async Task<IEnumerable<DocumentProgress>> GetUserDocumentsAsync(string username)
    {
        UserDocument? user = await GetUserByUsernameAsync(username);
        return user?.GetAllDocuments() ?? new List<DocumentProgress>();
    }

    public async Task<bool> UpdateMultipleDocumentsAsync(string username, Dictionary<string, DocumentProgress> documents)
    {
        UserDocument? user = await GetUserByUsernameAsync(username);
        if (user == null) return false;

        foreach (KeyValuePair<string, DocumentProgress> kvp in documents)
        {
            user.AddOrUpdateDocument(kvp.Value);
        }

        bool result = await UpdateUserAsync(user);
        if (result)
        {
            _logger.LogDebug("Updated {DocumentCount} documents for user {Username}", documents.Count, username);
        }
        return result;
    }

    // Advanced queries
    public async Task<IEnumerable<UserDocument>> GetUsersWithDocumentAsync(string documentHash)
    {
        IEnumerable<UserRow> results = await _db.Query(Tables.Users)
            .Where(UserColumns.Documents, "like", $"%\"{documentHash}\":%")
            .GetAsync<UserRow>();
        
        return results.Select(r => r.ToUserDocument()).Where(u => u.Documents.ContainsKey(documentHash));
    }

    public async Task<long> GetTotalDocumentCountAsync()
    {
        IEnumerable<UserDocument> users = await GetAllUsersAsync();
        return users.Sum(u => u.Documents.Count);
    }
}

internal class UserRow
{
    public string Id { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public int IsActive { get; set; }
    public int IsAdministrator { get; set; }
    public string Documents { get; set; } = default!;

    public UserDocument ToUserDocument()
    {
        Dictionary<string, DocumentProgress> documents = string.IsNullOrEmpty(Documents) || Documents == "{}" 
            ? new Dictionary<string, DocumentProgress>()
            : JsonSerializer.Deserialize<Dictionary<string, DocumentProgress>>(Documents) ?? new Dictionary<string, DocumentProgress>();

        return new UserDocument
        {
            Id = Id,
            Username = Username,
            PasswordHash = PasswordHash,
            IsActive = IsActive == 1,
            IsAdministrator = IsAdministrator == 1,
            Documents = documents
        };
    }

    public static UserRow FromUserDocument(UserDocument user)
    {
        return new UserRow
        {
            Id = user.Id ?? Guid.NewGuid().ToString(),
            Username = user.Username,
            PasswordHash = user.PasswordHash,
            IsActive = user.IsActive ? 1 : 0,
            IsAdministrator = user.IsAdministrator ? 1 : 0,
            Documents = JsonSerializer.Serialize(user.Documents)
        };
    }
}