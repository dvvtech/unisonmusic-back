using Microsoft.Extensions.Options;
using Unisonmusic.Api.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Unisonmusic.Api.DAL
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDAL(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));

            services.AddDbContextFactory<UnisonmusicDbContext>((serviceProvider, options) =>
            {
                //var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
                var dbOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

                var env = serviceProvider.GetRequiredService<IWebHostEnvironment>();
                if (!env.IsDevelopment())
                {
                    var cs = GetConnectionStringFromSecret();
                    //if (logger != null)
                    //{
                    //    logger.LogInformation("connection string: " + cs);
                    //}
                    options.UseNpgsql(cs);
                }
                else
                {
                    options.UseNpgsql(dbOptions.ConnectionString);
                }
            });

            return services;
        }

        private static string GetConnectionStringFromSecret()
        {
            var secretsPath = "/run/secrets";
            var ipFile = Path.Combine(secretsPath, "unisonmusic_connection_string");
            if (File.Exists(ipFile))
            {
                return File.ReadAllText(ipFile).Trim();
            }
            return "";
        }
    }
}
