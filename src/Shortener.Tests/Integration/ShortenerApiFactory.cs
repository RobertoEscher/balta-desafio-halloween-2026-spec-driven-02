using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shortener.Api.Data;
using Shortener.Api.Services;

namespace Shortener.Tests.Integration;

public sealed class ShortenerApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"shortener-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<LinkDbContext>>();
            services.RemoveAll<LinkDbContext>();
            services.AddDbContext<LinkDbContext>(options =>
                options.UseSqlite($"Data Source={_databasePath};Default Timeout=30;Pooling=False"));

            services.RemoveAll<IUrlValidator>();
            services.AddSingleton<IUrlValidator, AccessibleUrlValidator>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }

    private sealed class AccessibleUrlValidator : IUrlValidator
    {
        public Task<string?> ValidateAsync(string? value, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(null);
    }
}
