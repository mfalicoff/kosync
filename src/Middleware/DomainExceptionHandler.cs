using System;
using System.Threading;
using System.Threading.Tasks;
using Kosync.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Kosync.Middleware;

internal sealed class DomainExceptionHandler(ILogger<DomainExceptionHandler> logger)
    : IExceptionHandler
{
    private readonly ILogger<DomainExceptionHandler> _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        if (exception is not DomainException domainException)
        {
            return false; // Let the next handler deal with it
        }

        _logger.LogWarning(
            domainException,
            "Domain exception occurred: {Message}",
            domainException.Message
        );

        (int statusCode, string title) = domainException switch
        {
            UserAlreadyExistsException => (StatusCodes.Status409Conflict, "Conflict"),
            RegistrationDisabledException => (StatusCodes.Status403Forbidden, "Forbidden"),
            UserNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            _ => (StatusCodes.Status400BadRequest, "Bad Request"),
        };

        ProblemDetails problemDetails = new()
        {
            Status = statusCode,
            Title = title,
            Detail = domainException.Message,
            Type = domainException.GetType().Name,
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
