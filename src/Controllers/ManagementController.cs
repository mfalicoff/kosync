using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Kosync.Auth;
using Kosync.Database;
using Kosync.Database.Entities;
using Kosync.Extensions;
using Kosync.Models;
using Kosync.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Kosync.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class ManagementController(ILogger<ManagementController> logger, ProxyService proxyService, IPService ipService, IHttpContextAccessor contextAccessor, IKosyncRepository kosyncRepository)
    : ControllerBase
{
    private readonly ILogger<ManagementController> _logger = logger;
    private readonly ProxyService _proxyService = proxyService;
    private readonly IPService _ipService = ipService;
    private readonly IKosyncRepository _kosyncRepository = kosyncRepository;
    private readonly IHttpContextAccessor _contextAccessor = contextAccessor;

    private ClaimsPrincipal UserPrincipal => _contextAccessor.HttpContext?.User ?? throw new InvalidOperationException("HttpContext is not available.");
    
    [HttpGet("/manage/users")]
    public async Task<ObjectResult> GetUsers()
    {
        // Get all users (without sensitive data like password hashes)
        IEnumerable<UserDocument> users = await _kosyncRepository.GetAllUsersAsync();
        
        // Transform to a safe representation
        var userList = users.Select(u => new
        {
            id = u.Id,
            username = u.Username,
            isActive = u.IsActive,
            isAdministrator = u.IsAdministrator,
            documentCount = u.Documents.Count
        });

        LogInfo($"User [{UserPrincipal.Username()}] requested /manage/users");
        return StatusCode(200, userList);
    }

    [HttpPost("/manage/users")]
    public async Task<ObjectResult> CreateUser(UserCreateRequest payload)
    {
        UserDocument? user = await _kosyncRepository.GetUserByUsernameAsync(payload.username);
        
        if (user is not null)
        {
            return StatusCode(400, new
            {
                message = "User already exists"
            });
        }

        var passwordHash = Utility.HashPassword(payload.password);

        user = new UserDocument
        {
            Username = payload.username,
            PasswordHash = passwordHash,
            IsAdministrator = false,
            IsActive = true
        };

        var success = await _kosyncRepository.CreateUserAsync(user);
        if (!success)
        {
            return StatusCode(500, new
            {
                message = "Failed to create user"
            });
        }

        LogInfo($"User [{payload.username}] created by user [{UserPrincipal.Username()}]");
        return StatusCode(200, new
        {
            message = "User created successfully"
        });
    }

    [HttpDelete("/manage/users")]
    public async Task<ObjectResult> DeleteUser(string username)
    {
        UserDocument? user = await _kosyncRepository.GetUserByUsernameAsync(username);
        if (user == null)
        {
            LogWarning($"User [{username}] deletion requested by [{UserPrincipal.Username()}] but user does not exist.");
            return StatusCode(400, new
            {
                message = "User does not exist"
            });
        }

        var res = await _kosyncRepository.DeleteUserAsync(user.Id!);
        if (!res)
        {
            return StatusCode(500, new
            {
                message = "Failed to delete user"
            });
        }

        LogInfo($"User [{username}] has been deleted by [{UserPrincipal.Username()}]");

        return StatusCode(200, new
        {
            message = "Success"
        });
    }

    [HttpGet("/manage/users/documents")]
    public async Task<ObjectResult> GetDocuments(string username)
    {
        LogInfo($"User [{username}]'s documents requested by [{UserPrincipal.Username()}]");

        UserDocument? user = await _kosyncRepository.GetUserByUsernameAsync(username);
        if (user is null)
        {
            return StatusCode(400, new
            {
                message = "User does not exist"
            });
        }

        // Convert dictionary to list for API response
        var documents = user.Documents.Values.Select(doc => new
        {
            documentHash = doc.DocumentHash,
            progress = doc.Progress,
            percentage = doc.Percentage,
            device = doc.Device,
            deviceId = doc.DeviceId,
            timestamp = doc.Timestamp
        });

        return StatusCode(200, documents);
    }

    [HttpDelete("/manage/users/documents")]
    public async Task<ObjectResult> DeleteUserDocument(string username, string documentHash)
    {
        UserDocument? user = await _kosyncRepository.GetUserByUsernameAsync(username);
        if (user is null)
        {
            return StatusCode(400, new
            {
                message = "User does not exist"
            });
        }

        if (!user.Documents.ContainsKey(documentHash))
        {
            return StatusCode(404, new
            {
                message = $"Document hash [{documentHash}] was not found for user [{username}]."
            });
        }

        var success = await _kosyncRepository.RemoveDocumentAsync(username, documentHash);
        if (!success)
        {
            return StatusCode(500, new
            {
                message = "Failed to delete document"
            });
        }

        LogInfo($"User [{UserPrincipal.Username()}] deleted document with hash [{documentHash}] for user [{username}].");

        return StatusCode(200, new
        {
            message = "Success"
        });
    }

    [HttpPut("/manage/users/active")]
    public async Task<ObjectResult> UpdateUserActive(string username)
    {
        if (username == "admin")
        {
            LogWarning($"Attempt to toggle admin user active from user [{UserPrincipal.Username()}].");

            return StatusCode(400, new
            {
                message = "Cannot update admin user"
            });
        }

        UserDocument? user = await _kosyncRepository.GetUserByUsernameAsync(username);
        if (user is null)
        {
            LogInfo($"PUT request to /manage/users/active received from [{UserPrincipal.Username()}] but target username [{username}] does not exist.");

            return StatusCode(400, new
            {
                message = "User does not exist"
            });
        }

        user.IsActive = !user.IsActive;
        var success = await _kosyncRepository.UpdateUserAsync(user);
        if (!success)
        {
            return StatusCode(500, new
            {
                message = "Failed to update user"
            });
        }

        LogInfo($"User [{username}] set to {(user.IsActive ? "active" : "inactive")} by user [{UserPrincipal.Username()}]");

        return StatusCode(200, new
        {
            message = user.IsActive ? "User marked as active" : "User marked as inactive"
        });
    }

    [HttpPut("/manage/users/password")]
    public async Task<ObjectResult> UpdatePassword(string username, PasswordChangeRequest payload)
    {
        // KOReader will literally not attempt to log in with a blank password field or with just whitespace
        if (string.IsNullOrWhiteSpace(payload.password))
        {
            return StatusCode(400, new
            {
                message = "Password cannot be empty or whitespace"
            });
        }

        if (username == "admin")
        {
            LogWarning($"Attempt to change admin password from user [{UserPrincipal.Username()}].");
            return StatusCode(400, new
            {
                message = "Cannot update admin user"
            });
        }

        UserDocument? user = await _kosyncRepository.GetUserByUsernameAsync(username);
        if (user is null)
        {
            LogWarning($"Password change request received from [{UserPrincipal.Username()}] but target username [{username}] does not exist.");
            return StatusCode(400, new
            {
                message = "User does not exist"
            });
        }

        user.PasswordHash = Utility.HashPassword(payload.password);
        var success = await _kosyncRepository.UpdateUserAsync(user);
        if (!success)
        {
            return StatusCode(500, new
            {
                message = "Failed to update password"
            });
        }

        LogInfo($"User [{username}]'s password updated by [{UserPrincipal.Username()}].");
        return StatusCode(200, new
        {
            message = "Password changed successfully"
        });
    }

    private void LogInfo(string text)
    {
        Log(LogLevel.Information, text);
    }

    private void LogWarning(string text)
    {
        Log(LogLevel.Warning, text);
    }

    private void Log(LogLevel level, string text)
    {
        string logMsg = $"[{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}] [{_ipService.ClientIP}]";

        // If trusted proxies are set but this request comes from another address, mark it
        if (_proxyService.TrustedProxies.Length > 0 &&
            !_ipService.TrustedProxy)
        {
            logMsg += "*";
        }

        logMsg += $" {text}";

        _logger?.Log(level, logMsg);
    }
}