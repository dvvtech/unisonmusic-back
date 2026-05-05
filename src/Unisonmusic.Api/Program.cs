using Unisonmusic.Api.AppStart;
using Unisonmusic.Api.AppStart.Extensions;

var builder = WebApplication.CreateBuilder(args);

var startup = new Startup(builder);
startup.Initialize();

var app = builder.Build();

if (builder.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.ApplyCors();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
