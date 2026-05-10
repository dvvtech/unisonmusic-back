using System.Text.Json;
using Unisonmusic.Api.AppStart.Extensions;
using Unisonmusic.Api.Configuration;
using Unisonmusic.Api.DAL;
using Unisonmusic.Api.Services;
using Unisonmusic.Api.Services.Abstract;

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
            if (_builder.Environment.IsDevelopment())
            {
                _builder.Services.AddSwaggerGen();
            }
            //else
            //{
                _builder.Services.ConfigureCors();
            //}

            InitConfigs();
            ConfigureServices();
            //SetupDb();

            _builder.Services.AddControllers();            
        }

        private void InitConfigs()
        {
            if (!_builder.Environment.IsDevelopment())
            {
                _builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);
            }

            _builder.Services.Configure<GoogleRecaptchaConfig>(_builder.Configuration.GetSection(GoogleRecaptchaConfig.SectionName));
            _builder.Services.Configure<S3CloudConfig>(_builder.Configuration.GetSection(S3CloudConfig.SectionName));
            _builder.Services.Configure<DatabaseOptions>(_builder.Configuration.GetSection(DatabaseOptions.SectionName));
        }

        private void SetupDb()
        {
            _builder.Services.AddDAL(_builder.Configuration);
        }

        private void ConfigureServices()
        {
            _builder.Services.AddSingleton<IRoomService, RoomService>();
            _builder.Services.AddScoped<IOfftubeClient, OfftubeClient>();
            _builder.Services.AddScoped<IStorageService, S3StorageService>();            

            _builder.Services.AddHttpClient<IOfftubeClient, OfftubeClient>((serviceProvider, client) =>
            {
                var config = _builder.Configuration.GetSection(GoogleRecaptchaConfig.SectionName).Get<GoogleRecaptchaConfig>();

                client.BaseAddress = new Uri("http://offtube_api:8080");
                client.Timeout = TimeSpan.FromSeconds(45); // Таймаут запроса
                //client.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.SecretKeyForOfftube}");
            });

            _builder.Services
                .AddSignalR()
                .AddJsonProtocol(options =>
                {
                    // SignalR client code expects camelCase JSON fields.
                    options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                });
        }
    }
}
