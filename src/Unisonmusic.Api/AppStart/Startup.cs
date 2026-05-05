using System.Text.Json;
using Unisonmusic.Api.AppStart.Extensions;
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
        }

        private void ConfigureServices()
        {
            _builder.Services.AddSingleton<IRoomService, RoomService>();
        }
    }
}
