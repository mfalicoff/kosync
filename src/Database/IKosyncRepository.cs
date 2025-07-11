using System.Collections.Generic;
using System.Threading.Tasks;
using Kosync.Database.Entities;

namespace Kosync.Database;

public interface IKosyncRepository
{
    // User operations
    Task<UserDocument?> GetUserByUsernameAsync(string username);
    Task<UserDocument?> GetUserByIdAsync(string id);
    Task<IEnumerable<UserDocument>> GetAllUsersAsync();
    Task<bool> CreateUserAsync(UserDocument user);
    Task<bool> UpdateUserAsync(UserDocument user);
    Task<bool> DeleteUserAsync(string username);

    // Document operations
    Task<bool> AddOrUpdateDocumentAsync(string username, DocumentProgress document);
    Task<DocumentProgress?> GetDocumentAsync(string username, string documentHash);
    Task<bool> RemoveDocumentAsync(string username, string documentHash);
    Task<IEnumerable<DocumentProgress>> GetUserDocumentsAsync(string username);
    Task<bool> UpdateMultipleDocumentsAsync(string username, Dictionary<string, DocumentProgress> documents);

    // Advanced queries
    Task<IEnumerable<UserDocument>> GetUsersWithDocumentAsync(string documentHash);
    Task<long> GetTotalDocumentCountAsync();
}
