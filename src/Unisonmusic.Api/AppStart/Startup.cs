using System.Text.Json;
using Unisonmusic.Api.AppStart.Extensions;
using Unisonmusic.Api.Configuration;
using Unisonmusic.Api.Services;

namespace Unisonmusic.Api.AppStart
{
    public class Startup
    {
        private readonly WebApplicationBuilder _builder;

        public Startup(WebApplicationBuilder builder)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        public void Initialize()
        {
            _builder.Services.AddSwaggerGen();
            _builder.Services.ConfigureCors();

            InitConfigs();
            ConfigureServices();

            _builder.Services.AddControllers();
            _builder.Services
                .AddSignalR()
                .AddJsonProtocol(options =>
                {
                    // SignalR client code expects camelCase JSON fields.
                    options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                });
        }

        private void InitConfigs()
        {
            if (!_builder.Environment.IsDevelopment())
            {
                _builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);
            }

            var configSection = _builder.Configuration.GetSection(GoogleRecaptchaConfig.SectionName);
        }

        private void ConfigureServices()
        {
            _builder.Services.AddSingleton<IRoomService, RoomService>();
            _builder.Services.AddScoped<IOfftubeClient, OfftubeClient>();            
        }
    }
}
