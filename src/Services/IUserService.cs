using System.Collections.Generic;
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

    Task DeleteUserAsync(string username, CancellationToken cancellationToken = default);

    Task UpdateUserStatusAsync(
        string username,
        bool isActive,
        CancellationToken cancellationToken = default
    );

    Task UpdateUserPasswordAsync(
        string username,
        string newPassword,
        CancellationToken cancellationToken = default
    );

    Task<IEnumerable<UserDocument>> GetUserDocumentsAsync(
        CancellationToken cancellationToken = default
    );
}
