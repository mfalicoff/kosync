using System;
using System.Threading;
using System.Threading.Tasks;
using Kosync.Database;
using Kosync.Database.Entities;
using Kosync.Models;
using Microsoft.Extensions.Logging;

namespace Kosync.Services;

public class SyncService(IKosyncRepository repository, ILogger<SyncService> logger) : ISyncService
{
    private readonly IKosyncRepository _repository = repository;
    private readonly ILogger<SyncService> _logger = logger;

    public async Task<DocumentProgress?> UpdateProgressAsync(
        string username,
        string documentHash,
        DocumentRequest documentProgressRequest,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation(
            "Updating progress for document {DocumentHash} for user {Username}",
            documentHash,
            username
        );

        DocumentProgress? document = await _repository.GetDocumentAsync(username, documentHash);

        if (document == null)
        {
            _logger.LogInformation(
                "No existing progress found for document {DocumentHash} for user {Username}",
                documentHash,
                username
            );
            document = new DocumentProgress { DocumentHash = documentHash };
        }

        document.Progress = documentProgressRequest.progress;
        document.Percentage = documentProgressRequest.percentage;
        document.Device = documentProgressRequest.device;
        document.DeviceId = documentProgressRequest.device_id;
        document.Timestamp = DateTime.UtcNow;

        var result = await repository.AddOrUpdateDocumentAsync(username, document);
        if (!result)
        {
            _logger.LogError(
                "Failed to update progress for document {DocumentHash} for user {Username}",
                documentHash,
                username
            );
            return null;
        }

        return document;
    }

    public async Task<DocumentProgress?> GetDocumentProgressAsync(
        string username,
        string documentHash,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation(
            "Retrieving progress for document {DocumentHash} for user {Username}",
            documentHash,
            username
        );

        DocumentProgress? documentProgress = await _repository.GetDocumentAsync(
            username,
            documentHash
        );

        if (documentProgress == null)
        {
            _logger.LogWarning(
                "No progress found for document {DocumentHash} for user {Username}",
                documentHash,
                username
            );
        }

        return documentProgress;
    }
}
