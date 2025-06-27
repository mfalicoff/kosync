using System.Net;
using Kosync.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();


builder.Services.AddSingleton<ProxyService, ProxyService>();
builder.Services.AddScoped<IPService, IPService>();
builder.Services.AddScoped<KosyncDb, KosyncDb>();
builder.Services.AddKoreaderAuth();


builder.Services.AddControllers();


if (Environment.GetEnvironmentVariable("SINGLE_LINE_LOGGING") == "true")
{

    builder.Logging.ClearProviders();
    builder.Logging.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
    });
}


var app = builder.Build();

app.UseForwardedHeaders();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();



app.Run();
