using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace Kosync.Database.Entities;

public class UserDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("username")]
    public string Username { get; set; } = default!;

    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = default!;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("isAdministrator")]
    public bool IsAdministrator { get; set; } = false;

    [BsonElement("documents")]
    public Dictionary<string, DocumentProgress> Documents { get; set; } = new();

    // Helper methods for document management
    public void AddOrUpdateDocument(DocumentProgress document)
    {
        Documents[document.DocumentHash] = document;
    }

    public DocumentProgress? GetDocument(string documentHash)
    {
        return Documents.TryGetValue(documentHash, out DocumentProgress? document) ? document : null;
    }

    public bool RemoveDocument(string documentHash)
    {
        return Documents.Remove(documentHash);
    }

    public IEnumerable<DocumentProgress> GetAllDocuments()
    {
        return Documents.Values;
    }
}

public class DocumentProgress
{
    [BsonElement("documentHash")]
    public string DocumentHash { get; set; } = default!;

    [BsonElement("progress")]
    public string Progress { get; set; } = default!;

    [BsonElement("percentage")]
    public decimal Percentage { get; set; } = default!;

    [BsonElement("device")]
    public string Device { get; set; } = default!;

    [BsonElement("deviceId")]
    public string DeviceId { get; set; } = default!;

    [BsonElement("timestamp")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime Timestamp { get; set; } = default!;
}
