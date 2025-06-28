using Kosync.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Kosync.Endpoints.Management;

public static class ManagementEndpoints
{
    public static void Map(WebApplication app)
    {
        RouteGroupBuilder managementGroup = app.MapGroup("/v2/manage")
            .WithTags("Manage")
            .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        UserManagementEndpoints.Map(managementGroup);
        DocumentManagementEndpoints.Map(managementGroup);
    }
}
