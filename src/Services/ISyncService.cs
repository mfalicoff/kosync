using System.Threading;
using System.Threading.Tasks;
using Kosync.Database.Entities;
using Kosync.Endpoints;
using Kosync.Models;

namespace Kosync.Services;

public interface ISyncService
{
    Task<DocumentProgress?> UpdateProgressAsync(
        string username,
        string documentHash,
        DocumentRequest documentProgressRequest,
        CancellationToken cancellationToken = default
    );

    Task<DocumentProgress?> GetDocumentProgressAsync(
        string username,
        string documentHash,
        CancellationToken cancellationToken = default
    );
}
