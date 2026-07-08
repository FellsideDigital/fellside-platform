using FellsideDigital.Web.Data;
using FellsideDigital.Web.Extensions;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Configure app platform (services + auth)
builder.Services.AddFellsideDigitalPlatform(builder.Configuration, builder.Environment);

// Persist data protection keys in the database so auth cookies remain decryptable
// across deploys/restarts (a fresh container filesystem — e.g. Railway — otherwise
// regenerates the keys and signs every user out on each deploy).
builder.Services
    .AddDataProtection()
    .PersistKeysToDbContext<FellsideDigitalDbContext>()
    .SetApplicationName("FellsideDigital");

var app = builder.Build();

// Apply migrations and seed
await app.ApplyStartupTasksAsync();

// Use platform pipeline
app.UseFellsideDigitalPlatform();

app.Run();
