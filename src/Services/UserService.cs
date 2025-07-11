using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kosync.Database;
using Kosync.Database.Entities;
using Kosync.Exceptions;
using Kosync.Extensions;
using Microsoft.Extensions.Logging;

namespace Kosync.Services;

public class UserService(IKosyncRepository kosyncRepository, ILogger<UserService> logger) : IUserService
{
    private readonly IKosyncRepository _kosyncRepository = kosyncRepository;

    private readonly ILogger<UserService> _logger = logger;

    public async Task CreateUserAsync(string username, string password, CancellationToken token = default)
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

    public Task<UserDocument?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return _kosyncRepository.GetUserByUsernameAsync(username);
    }

    public async Task DeleteUserAsync(string username, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting user with username: {Username}", username);
        UserDocument? user = await _kosyncRepository.GetUserByUsernameAsync(username);

        if (user == null)
        {
            _logger.LogError("User not found: {Username}", username);
            throw new UserNotFoundException(username);
        }

        user.ThrowIfNotAdmin();

        bool result = await _kosyncRepository.DeleteUserAsync(username);
        if (!result)
        {
            _logger.LogError("Failed to delete user with username: {Username}", username);
            throw new UserNotFoundException(username);
        }
    }

    public async Task UpdateUserStatusAsync(
        string username,
        bool isActive,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("Updating status for user: {Username}", username);

        UserDocument? user = await _kosyncRepository.GetUserByUsernameAsync(username);
        if (user == null)
        {
            _logger.LogError("User not found: {Username}", username);
            throw new UserNotFoundException(username);
        }

        user.ThrowIfNotAdmin();

        user.IsActive = isActive;
        bool result = await _kosyncRepository.UpdateUserAsync(user);

        if (!result)
        {
            throw new Exception("Failed to update user");
        }
    }

    public async Task UpdateUserPasswordAsync(
        string username,
        string newPassword,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("Updating password for user: {Username}", username);

        UserDocument? user = await _kosyncRepository.GetUserByUsernameAsync(username);
        if (user == null)
        {
            _logger.LogError("User not found: {Username}", username);
            throw new UserNotFoundException(username);
        }

        user.ThrowIfNotAdmin();

        user.PasswordHash = newPassword;
        bool result = await _kosyncRepository.UpdateUserAsync(user);

        if (!result)
        {
            _logger.LogError("Failed to update password for user: {Username}", username);
            throw new Exception("Failed to update user password");
        }
    }

    public Task<IEnumerable<UserDocument>> GetUserDocumentsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving all user documents");

        return _kosyncRepository.GetAllUsersAsync();
    }
}
