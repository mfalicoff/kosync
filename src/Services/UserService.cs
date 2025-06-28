using System;
using System.Threading;
using System.Threading.Tasks;
using Kosync.Database;
using Kosync.Database.Entities;
using Kosync.Exceptions;
using Kosync.Middleware;

namespace Kosync.Services;

public class UserService(IKosyncRepository kosyncRepository) : IUserService
{
    private readonly IKosyncRepository _kosyncRepository = kosyncRepository;

    public async Task CreateUserAsync(
        string username,
        string password,
        CancellationToken token = default
    )
    {
        if (Environment.GetEnvironmentVariable("REGISTRATION_DISABLED") == "true")
        {
            throw new RegistrationDisabledException();
        }

        UserDocument? existing = await _kosyncRepository.GetUserByUsernameAsync(username);

        if (existing is not null)
        {
            throw new UserAlreadyExistsException(existing.Username);
        }

        bool result = await _kosyncRepository.CreateUserAsync(
            new UserDocument
            {
                Username = username,
                PasswordHash = password,
                IsAdministrator = false,
                IsActive = true,
            }
        );

        if (!result)
        {
            throw new Exception("Failed to create user");
        }
    }

    public Task<UserDocument?> GetUserByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default
    )
    {
        return _kosyncRepository.GetUserByUsernameAsync(username);
    }
}
