using System;
using Kosync.Endpoints;
using Kosync.Endpoints.Management;
using Kosync.Extensions;
using Kosync.Middleware;
using Kosync.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MongoDbOptions>(
    builder.Configuration.GetSection(MongoDbOptions.SectionName)
);

builder.Services.AddHttpContextAccessor();

builder.Services.AddIpDetection();
builder.Services.AddKoreaderAuth();
builder.Services.AddMongoDb(builder.Configuration.GetRequiredSection<MongoDbOptions>());
builder.Services.AddTransient<ISyncService, SyncService>();
builder.Services.AddTransient<IUserService, UserService>();

builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

if (Environment.GetEnvironmentVariable("SINGLE_LINE_LOGGING") == "true")
{
    builder.Logging.ClearProviders();
    builder.Logging.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
    });
}

WebApplication app = builder.Build();

app.UseForwardedHeaders();

app.UseIpDetection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

SyncEndpoints.Map(app);
HealthCheckEndpoints.Map(app);
AuthEndpoint.Map(app);
ManagementEndpoints.Map(app);

app.UseExceptionHandler();

app.Run();
