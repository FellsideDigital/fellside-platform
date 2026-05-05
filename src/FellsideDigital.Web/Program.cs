using FellsideDigital.Web.Extensions;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Configure app platform (services + auth)
builder.Services.AddFellsideDigitalPlatform(builder.Configuration);

// Persist data protection keys to survive container restarts
var keysFolder = "/app/keys";
Directory.CreateDirectory(keysFolder);
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
    .SetApplicationName("FellsideDigital");

var app = builder.Build();

// Apply migrations and seed
await app.ApplyStartupTasksAsync();

// Use platform pipeline
app.UseFellsideDigitalPlatform();

app.Run();
