using System.Security.Claims;
using Kosync.Auth;
using Kosync.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kosync.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class ManagementController(ILogger<ManagementController> logger, ProxyService proxyService, IPService ipService, KosyncDb db, IHttpContextAccessor contextAccessor)
    : ControllerBase
{
    private readonly ILogger<ManagementController> _logger = logger;

    private readonly ProxyService _proxyService = proxyService;
    
    private readonly IPService _ipService = ipService;
    
    private readonly KosyncDb _db = db;

    private readonly IHttpContextAccessor _contextAccessor = contextAccessor;

    private ClaimsPrincipal UserPrincipal => _contextAccessor.HttpContext?.User ?? throw new InvalidOperationException("HttpContext is not available.");
    
    [HttpGet("/manage/users")]
    public ObjectResult GetUsers()
    {
        ILiteCollection<User>? userCollection = _db.Context.GetCollection<User>("users");

        var users = userCollection.FindAll().Select(i => new
        {
            id = i.Id,
            username = i.Username,
            isAdministrator = i.IsAdministrator,
            isActive = i.IsActive,
            documentCount = i.Documents.Count()
        });

        LogInfo($"User [{UserPrincipal.Username()}] requested /manage/users");
        return StatusCode(200, users);
    }

    [HttpPost("/manage/users")]
    public ObjectResult CreateUser(UserCreateRequest payload)
    {
        ILiteCollection<User>? userCollection = _db.Context.GetCollection<User>("users");

        User? existingUser = userCollection.FindOne(i => i.Username == payload.username);
        if (existingUser is not null)
        {
            return StatusCode(400, new
            {
                message = "User already exists"
            });
        }

        var passwordHash = Utility.HashPassword(payload.password);

        var user = new User()
        {
            Username = payload.username,
            PasswordHash = passwordHash,
            IsAdministrator = false
        };

        userCollection.Insert(user);
        userCollection.EnsureIndex(u => u.Username);

        LogInfo($"User [{payload.username}] created by user [{UserPrincipal.Username()}]");
        return StatusCode(200, new
        {
            message = "User created successfully"
        });
    }

    [HttpDelete("/manage/users")]
    public ObjectResult DeleteUser(string username)
    {
        ILiteCollection<User>? userCollection = _db.Context.GetCollection<User>("users");

        User? user = userCollection.FindOne(u => u.Username == username);

        if (user is null)
        {
            LogInfo($"DELETE request to /manage/users received from [{UserPrincipal.Username()}] but target username [{username}] does not exist.");

            return StatusCode(404, new
            {
                message = "User does not exist"
            });
        }

        userCollection.Delete(user.Id);

        LogInfo($"User [{username}] has been deleted by [{UserPrincipal.Username()}]");

        return StatusCode(200, new
        {
            message = "Success"
        });
    }

    [HttpGet("/manage/users/documents")]
    public ObjectResult GetDocuments(string username)
    {
        LogInfo($"User [{username}]'s documents requested by [{UserPrincipal.Username()}]");

        ILiteCollection<User>? userCollection = _db.Context.GetCollection<User>("users");

        User? user = userCollection.FindOne(i => i.Username == username);
        if (user is null)
        {
            return StatusCode(400, new
            {
                message = "User does not exist"
            });
        }

        return StatusCode(200, user.Documents);
    }

    [HttpDelete("/manage/users/documents")]
    public ObjectResult DeleteUserDocument(string username, string documentHash)
    {
        ILiteCollection<User>? userCollection = _db.Context.GetCollection<User>("users").Include(i => i.Documents);

        User? user = userCollection.FindOne(i => i.Username == username);

        Document? document = user.Documents.SingleOrDefault(i => i.DocumentHash == documentHash);

        if (document is null)
        {
            return StatusCode(404, new
            {
                message = $"Document hash [{documentHash}] was not found for user [{username}]."
            });
        }

        user.Documents.Remove(document);

        userCollection.Update(user);

        LogInfo($"User [{UserPrincipal.Username()}] deleted document with hash [{documentHash}] for user [{username}].");

        return StatusCode(200, new
        {
            message = "Success"
        });
    }

    [HttpPut("/manage/users/active")]
    public ObjectResult UpdateUserActive(string username)
    {
        if (username == "admin")
        {
            LogWarning($"Attempt to toggle admin user active from user [{UserPrincipal.Username()}].");

            return StatusCode(400, new
            {
                message = "Cannot update admin user"
            });
        }

        ILiteCollection<User>? userCollection = _db.Context.GetCollection<User>("users");

        User? user = userCollection.FindOne(i => i.Username == username);
        if (user is null)
        {
            LogInfo($"PUT request to /manage/users/active received from [{UserPrincipal.Username()}] but target username [{username}] does not exist.");

            return StatusCode(400, new
            {
                message = "User does not exist"
            });
        }

        user.IsActive = !user.IsActive;
        userCollection.Update(user);

        LogInfo($"User [{username}] set to {(user.IsActive ? "active" : "inactive")} by user [{UserPrincipal.Username()}]");

        return StatusCode(200, new
        {
            message = user.IsActive ? "User marked as active" : "User marked as inactive"
        });
    }

    [HttpPut("/manage/users/password")]
    public ObjectResult UpdatePassword(string username, PasswordChangeRequest payload)
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

        ILiteCollection<User>? userCollection = _db.Context.GetCollection<User>("users");

        User? user = userCollection.FindOne(i => i.Username == username);
        if (user is null)
        {
            LogWarning($"Password change request received from [{UserPrincipal.Username()}] but target username [{username}] does not exist.");
            return StatusCode(400, new
            {
                message = "User does not exist"
            });
        }

        user.PasswordHash = Utility.HashPassword(payload.password);
        userCollection.Update(user);

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