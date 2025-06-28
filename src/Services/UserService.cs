using System;
using System.Threading;
using System.Threading.Tasks;
using Kosync.Database;
using Kosync.Database.Entities;
using Kosync.Exceptions;
using Kosync.Middleware;
using Microsoft.Extensions.Logging;

namespace Kosync.Services;

public class UserService(IKosyncRepository kosyncRepository, ILogger<UserService> logger)
    : IUserService
{
    private readonly IKosyncRepository _kosyncRepository = kosyncRepository;

    private readonly ILogger<UserService> _logger = logger;

    public async Task CreateUserAsync(
        string username,
        string password,
        CancellationToken token = default
    )
    {
        _logger.LogInformation("Creating user with username: {Username}", username);

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
