using System.Threading;
using System.Threading.Tasks;
using Kosync.Database.Entities;

namespace Kosync.Services;

public interface IUserService
{
    Task CreateUserAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default
    );

    Task<UserDocument?> GetUserByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default
    );
}
