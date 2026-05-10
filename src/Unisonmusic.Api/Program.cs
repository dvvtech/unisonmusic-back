using Unisonmusic.Api.AppStart;
using Unisonmusic.Api.AppStart.Extensions;
using Unisonmusic.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls("http://*:5113", "http://localhost:5113");
}

var startup = new Startup(builder);
startup.Initialize();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
//else
//{
    app.ApplyCors();
//}

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
