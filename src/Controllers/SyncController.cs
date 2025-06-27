using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Kosync.Database;
using Kosync.Database.Entities;
using Kosync.Extensions;
using Kosync.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Kosync.Controllers;

[ApiController]
public class SyncController(
    ILogger<SyncController> logger,
    IHttpContextAccessor contextAccessor,
    IKosyncRepository kosyncRepository
) : ControllerBase
{
    private readonly ILogger<SyncController> _logger = logger;

    private readonly IHttpContextAccessor _contextAccessor = contextAccessor;

    private readonly IKosyncRepository _kosyncRepository = kosyncRepository;

    private ClaimsPrincipal UserPrincipal =>
        _contextAccessor.HttpContext?.User
        ?? throw new InvalidOperationException("HttpContext is not available.");

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
        return StatusCode(200, new { state = "OK" });
    }

    [Authorize]
    [HttpGet("/users/auth")]
    public ObjectResult AuthoriseUser()
    {
        LogInfo($"User [{UserPrincipal.Username()}] logged in.");
        return StatusCode(200, new { username = UserPrincipal.Username() });
    }

    [AllowAnonymous]
    [HttpPost("/users/create")]
    public async Task<ObjectResult> CreateUser(UserCreateRequest payload)
    {
        if (Environment.GetEnvironmentVariable("REGISTRATION_DISABLED") == "true")
        {
            LogWarning("Account creation attempted but registration is disabled.");
            return StatusCode(402, new { message = "User registration is disabled" });
        }
        UserDocument? existing = await _kosyncRepository.GetUserByUsernameAsync(payload.username);

        if (existing is not null)
        {
            LogInfo($"Account creation attempted with existing username - [{payload.username}].");
            return StatusCode(402, new { message = "User already exists" });
        }

        await _kosyncRepository.CreateUserAsync(
            new UserDocument
            {
                Username = payload.username,
                PasswordHash = payload.password,
                IsAdministrator = false,
                IsActive = true,
            }
        );

        LogInfo($"User [{payload.username}] created.");
        return StatusCode(201, new { Username = payload.username });
    }

    [Authorize]
    [HttpPut("/syncs/progress")]
    public async Task<ObjectResult> SyncProgress(DocumentRequest payload)
    {
        DocumentProgress document =
            await _kosyncRepository.GetDocumentAsync(UserPrincipal.Username(), payload.document)
            ?? new DocumentProgress { DocumentHash = payload.document };

        document.Progress = payload.progress;
        document.Percentage = payload.percentage;
        document.Device = payload.device;
        document.DeviceId = payload.device_id;
        document.Timestamp = DateTime.UtcNow;

        await _kosyncRepository.AddOrUpdateDocumentAsync(UserPrincipal.Username(), document);

        LogInfo(
            $"Received progress update for user [{UserPrincipal.Username()}] from device [{payload.device}] with document hash [{payload.document}]."
        );
        return StatusCode(
            200,
            new { document = document.DocumentHash, timestamp = document.Timestamp }
        );
    }

    [Authorize]
    [HttpGet("/syncs/progress/{documentHash}")]
    public async Task<IActionResult> GetProgress(string documentHash)
    {
        DocumentProgress? document = await _kosyncRepository.GetDocumentAsync(
            UserPrincipal.Username(),
            documentHash
        );

        if (document is null)
        {
            LogInfo(
                $"Document hash [{documentHash}] not found for user [{UserPrincipal.Username()}]."
            );
            return StatusCode(502, new { message = "Document not found on server" });
        }

        LogInfo(
            $"Received progress request for user [{UserPrincipal.Username()}] with document hash [{documentHash}]."
        );

        DateTimeOffset time = new(document.Timestamp);

        var result = new
        {
            device = document.Device,
            device_id = document.DeviceId,
            document = document.DocumentHash,
            percentage = document.Percentage,
            progress = document.Progress,
            timestamp = time.ToUnixTimeSeconds(),
        };

        string json = System.Text.Json.JsonSerializer.Serialize(result);

        return new ContentResult()
        {
            Content = json,
            ContentType = "application/json",
            StatusCode = 200,
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
        string logMsg =
            $"[{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}] [{_contextAccessor.HttpContext?.GetClientIP()}]";

        // If trusted proxies are set but this request comes from another address, mark it
        if (!_contextAccessor.HttpContext?.IsTrustedProxy() ?? false)
        {
            logMsg += "*";
        }

        logMsg += $" {text}";

        _logger?.Log(level, logMsg);
    }
}
