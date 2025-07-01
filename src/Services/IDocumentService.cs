using System.Threading.Tasks;

namespace Kosync.Services;

public interface IDocumentService
{
    Task GetDocumentsAsync(string username);

    Task DeleteDocumentAsync(string username, string documentHash);
}
