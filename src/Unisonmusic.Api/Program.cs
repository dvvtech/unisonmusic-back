using Unisonmusic.Api.AppStart;
using Unisonmusic.Api.AppStart.Extensions;
using Unisonmusic.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);

var startup = new Startup(builder);
startup.Initialize();

var app = builder.Build();

app.ApplyCors();
app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
app.MapHub<MusicHub>("/hubs/music");

app.MapGet("/", () => Results.Ok(new
{
    service = "Unisonmusic API",
    hub = "/hubs/music",
    utcNow = DateTimeOffset.UtcNow
}));

app.Run();
