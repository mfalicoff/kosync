using System;
using Kosync.Database.Entities;

namespace Kosync.Extensions;

public static class UserExtensions
{
    public static void ThrowIfNotAdmin(this UserDocument user)
    {
        if (!user.IsAdministrator)
        {
            throw new UnauthorizedAccessException("User is not an administrator");
        }
    }
}
