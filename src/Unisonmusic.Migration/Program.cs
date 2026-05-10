
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Unisonmusic.Api.DAL;
using Unisonmusic.Migration;

var environmentName = args.FirstOrDefault() ??
                              Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
                              throw new InvalidOperationException("ASPNETCORE_ENVIRONMENT in not set");

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile($"appsettings.{environmentName}.json")
    .Build();

var serviceCollection = new ServiceCollection();
serviceCollection.AddSingleton<IConfiguration>(configuration);
serviceCollection.AddTransient<MigrationService>();
serviceCollection.AddDAL(configuration);

var serviceProvider = serviceCollection.BuildServiceProvider();

try
{
    var migrationService = serviceProvider.GetRequiredService<MigrationService>();
    await migrationService.MigrateAsync(CancellationToken.None);
}
catch (Exception ex)
{
    Console.WriteLine($"Critical error during migration: {ex.Message}");
    throw;
}