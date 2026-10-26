using Microsoft.EntityFrameworkCore;
using Shortener.Api.Data;
using Shortener.Api.Endpoints;
using Shortener.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddDbContext<LinkDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Links") ?? "Data Source=links.db;Default Timeout=30"));
builder.Services.AddScoped<ILinkRepository, LinkRepository>();
builder.Services.AddSingleton<ILinkCodeGenerator, LinkCodeGenerator>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpClient<IUrlValidator, UrlValidator>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(5);
}).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    ConnectCallback = PublicInternetConnector.ConnectAsync
});
builder.Services.AddScoped<LinkService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapLinkEndpoints();

await using (var scope = app.Services.CreateAsyncScope())
{
    var database = scope.ServiceProvider.GetRequiredService<LinkDbContext>();
    await database.Database.MigrateAsync();
}

app.Run();

public partial class Program
{
}
