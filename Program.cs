using System;
using Kosync.Extensions;
using Kosync.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MongoDbOptions>(
    builder.Configuration.GetSection(MongoDbOptions.SectionName));

builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<ProxyService, ProxyService>();
builder.Services.AddScoped<IPService, IPService>();
builder.Services.AddKoreaderAuth();
builder.Services.AddMongoDb(builder.Configuration.GetRequiredSection<MongoDbOptions>());

builder.Services.AddOpenApi();

builder.Services.AddControllers();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.Run();
