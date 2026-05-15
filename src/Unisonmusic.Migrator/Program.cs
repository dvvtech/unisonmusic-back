using Unisonmusic.Api.DAL;
using Unisonmusic.Migrator;

var builder = WebApplication.CreateBuilder(args);

var environmentName = args.FirstOrDefault() ??
                              Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
                              throw new InvalidOperationException("ASPNETCORE_ENVIRONMENT in not set");

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile($"appsettings.{environmentName}.json", optional: false, reloadOnChange: true);

builder.Services.AddTransient<MigrationService>();
builder.Services.AddDAL(builder.Configuration);

var app = builder.Build();

try
{
    var migrationService = app.Services.GetRequiredService<MigrationService>();
    await migrationService.MigrateAsync(CancellationToken.None);
}
catch (Exception ex)
{
    Console.WriteLine($"Critical error during migration: {ex.Message}");
    throw;
}