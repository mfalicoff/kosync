using System;
using System.Linq;
using System.Security.Claims;
using ClaimTypes = Kosync.Auth.ClaimTypes;

namespace Kosync.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string Username(this ClaimsPrincipal principal)
    {
        return principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value
            ?? throw new NullReferenceException();
    }
}
