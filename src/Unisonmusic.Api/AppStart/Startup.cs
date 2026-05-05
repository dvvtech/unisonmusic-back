using Unisonmusic.Api.AppStart.Extensions;

namespace Unisonmusic.Api.AppStart
{
    public class Startup
    {
        private WebApplicationBuilder _builder;

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
            else
            {
                _builder.Services.ConfigureCors();
            }

            InitConfigs();
            ConfigureServices();

            _builder.Services.AddControllers();
        }

        private void InitConfigs()
        {
            
        }

        private void ConfigureServices()
        {
            
        }
    }
}
