using System.Security.Claims;
using Kosync.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kosync.Controllers;

[ApiController]
public class SyncController(ILogger<SyncController> logger, ProxyService proxyService, IPService ipService, KosyncDb db, IHttpContextAccessor contextAccessor)
    : ControllerBase
{
    private readonly ILogger<SyncController> _logger = logger;

    private readonly ProxyService _proxyService = proxyService;
    
    private readonly IPService _ipService = ipService;
    
    private readonly KosyncDb _db = db;
    
    private readonly IHttpContextAccessor _contextAccessor = contextAccessor;
    
    private ClaimsPrincipal UserPrincipal => _contextAccessor.HttpContext?.User ?? throw new InvalidOperationException("HttpContext is not available.");
    
    [AllowAnonymous]
    [HttpGet("/")]
    public IActionResult Index()
    {
        return Ok("kosync-dotnet server is running.");
    }

    [AllowAnonymous]
    [HttpGet("/healthcheck")]
    public ObjectResult HealthCheck()
    {
        return StatusCode(200, new
        {
            state = "OK"
        });
    }

    [Authorize]
    [HttpGet("/users/auth")]
    public ObjectResult AuthoriseUser()
    {
        LogInfo($"User [{UserPrincipal.Username()}] logged in.");
        return StatusCode(200, new
        {
            username = UserPrincipal.Username()
        });
    }

    [AllowAnonymous]
    [HttpPost("/users/create")]
    public ObjectResult CreateUser(UserCreateRequest payload)
    {
        if (Environment.GetEnvironmentVariable("REGISTRATION_DISABLED") == "true")
        {
            LogWarning("Account creation attempted but registration is disabled.");
            return StatusCode(402, new
            {
                message = "User registration is disabled"
            });
        }

        ILiteCollection<User>? userCollection = _db.Context.GetCollection<User>("users");

        User? existing = userCollection.FindOne(u => u.Username == payload.username);
        if (existing is not null)
        {
            LogInfo($"Account creation attempted with existing username - [{payload.username}].");
            return StatusCode(402, new
            {
                message = "User already exists"
            });
        }

        var user = new User()
        {
            Username = payload.username,
            PasswordHash = payload.password,
        };

        userCollection.Insert(user);
        userCollection.EnsureIndex(u => u.Username);


        LogInfo($"User [{payload.username}] created.");
        return StatusCode(201, new
        {
            Username = payload.username
        });
    }

    [Authorize]
    [HttpPut("/syncs/progress")]
    public ObjectResult SyncProgress(DocumentRequest payload)
    {
        ILiteCollection<User>? userCollection = _db.Context.GetCollection<User>("users").Include(i => i.Documents);

        User? user = userCollection.FindOne(i => i.Username == UserPrincipal.Username());

        Document? document = user.Documents.SingleOrDefault(i => i.DocumentHash == payload.document);
        if (document is null)
        {
            document = new Document();
            document.DocumentHash = payload.document;
            user.Documents.Add(document);
        }

        document.Progress = payload.progress;
        document.Percentage = payload.percentage;
        document.Device = payload.device;
        document.DeviceId = payload.device_id;
        document.Timestamp = DateTime.UtcNow;

        userCollection.Update(user);

        LogInfo($"Received progress update for user [{UserPrincipal.Username()}] from device [{payload.device}] with document hash [{payload.document}].");
        return StatusCode(200, new
        {
            document = document.DocumentHash,
            timestamp = document.Timestamp
        });
    }

    [Authorize]
    [HttpGet("/syncs/progress/{documentHash}")]
    public IActionResult GetProgress(string documentHash)
    {
        ILiteCollection<User>? userCollection = _db.Context.GetCollection<User>("users").Include(i => i.Documents);

        User? user = userCollection.FindOne(i => i.Username == UserPrincipal.Username());

        Document? document = user.Documents.SingleOrDefault(i => i.DocumentHash == documentHash);

        if (document is null)
        {
            LogInfo($"Document hash [{documentHash}] not found for user [{UserPrincipal.Username()}].");
            return StatusCode(502, new
            {
                message = "Document not found on server"
            });
        }

        LogInfo($"Received progress request for user [{UserPrincipal.Username()}] with document hash [{documentHash}].");

        var time = new DateTimeOffset(document.Timestamp);

        var result = new
        {
            device = document.Device,
            device_id = document.DeviceId,
            document = document.DocumentHash,
            percentage = document.Percentage,
            progress = document.Progress,
            timestamp = time.ToUnixTimeSeconds()
        };

        string json = System.Text.Json.JsonSerializer.Serialize(result);

        return new ContentResult()
        {
            Content = json,
            ContentType = "application/json",
            StatusCode = 200
        };
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
