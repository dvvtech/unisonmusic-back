namespace Unisonmusic.Api.AppStart.Extensions
{
    public static class CorsExtensions
    {
        public const string FrontendPolicy = "FrontendCors";

        public static void ConfigureCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(FrontendPolicy, policy =>
                {
                    policy
                        // Allows local HTML files, localhost ports and a future frontend domain.
                        .SetIsOriginAllowed(_ => true)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });
        }

        public static void ApplyCors(this WebApplication app)
        {
            app.UseCors(FrontendPolicy);
        }
    }
}
