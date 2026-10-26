using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shortener.Api.Data;
using Shortener.Api.Domain;

namespace Shortener.Tests.Integration;

public sealed class LinkEndpointsTests : IClassFixture<ShortenerApiFactory>
{
    private readonly ShortenerApiFactory _factory;

    public LinkEndpointsTests(ShortenerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateLink_ReturnsCreatedAndLocation()
    {
        using var client = CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/links",
            new { targetUrl = "https://example.test/created" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var body = await response.Content.ReadFromJsonAsync<CreateLinkResponse>();
        Assert.NotNull(body);
        Assert.Equal(7, body.Code.Length);
        Assert.StartsWith("http://", body.ShortUrl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateLink_ReturnsConflictForDuplicateCustomCode()
    {
        using var client = CreateClient();
        var request = new { targetUrl = "https://example.test/", code = "CUSTOM1" };

        using var first = await client.PostAsJsonAsync("/api/links", request);
        using var duplicate = await client.PostAsJsonAsync("/api/links", request);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Redirect_Returns302ForExistingAnd404Or410ForOtherCodes()
    {
        await SeedExpiredLinkAsync();
        using var client = CreateClient();
        using var created = await client.PostAsJsonAsync(
            "/api/links",
            new { targetUrl = "https://example.test/redirect-target", code = "REDIR11" });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var redirect = await client.GetAsync("/REDIR11");
        using var missing = await client.GetAsync("/MISSI01");
        using var expired = await client.GetAsync("/EXPIRE1");

        Assert.Equal(HttpStatusCode.Redirect, redirect.StatusCode);
        Assert.Equal(new Uri("https://example.test/redirect-target"), redirect.Headers.Location);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(HttpStatusCode.Gone, expired.StatusCode);
    }

    [Fact]
    public async Task GetLinkStats_ReturnsPublicAccessCount()
    {
        using var client = CreateClient();
        using var created = await client.PostAsJsonAsync(
            "/api/links",
            new { targetUrl = "https://example.test/stats", code = "STATS11" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using (var redirect = await client.GetAsync("/STATS11"))
        {
            Assert.Equal(HttpStatusCode.Redirect, redirect.StatusCode);
        }

        using var response = await client.GetAsync("/api/links/STATS11/stats");
        var stats = await response.Content.ReadFromJsonAsync<LinkStatsResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, stats!.AccessCount);
    }

    [Fact]
    public async Task Redirect_CountsRepeatedAutomatedAndConcurrentAccesses()
    {
        using var client = CreateClient();
        using var created = await client.PostAsJsonAsync(
            "/api/links",
            new { targetUrl = "https://example.test/counts", code = "COUNT11" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using (var repeated = await client.GetAsync("/COUNT11"))
        {
            Assert.Equal(HttpStatusCode.Redirect, repeated.StatusCode);
        }

        using var automatedRequest = new HttpRequestMessage(HttpMethod.Get, "/COUNT11");
        automatedRequest.Headers.UserAgent.ParseAdd("ExampleCrawler/1.0");
        using (var automated = await client.SendAsync(automatedRequest))
        {
            Assert.Equal(HttpStatusCode.Redirect, automated.StatusCode);
        }

        await Parallel.ForEachAsync(
            Enumerable.Range(0, 40),
            async (_, cancellationToken) =>
            {
                using var response = await client.GetAsync("/COUNT11", cancellationToken);
                Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            });

        using var statsResponse = await client.GetAsync("/api/links/COUNT11/stats");
        var stats = await statsResponse.Content.ReadFromJsonAsync<LinkStatsResponse>();

        Assert.Equal(HttpStatusCode.OK, statsResponse.StatusCode);
        Assert.Equal(42, stats!.AccessCount);
    }

    public sealed record CreateLinkResponse(string ShortUrl, string Code);

    public sealed record LinkStatsResponse(string Code, long AccessCount);

    private HttpClient CreateClient() =>
        _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    private async Task SeedExpiredLinkAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<LinkDbContext>();
        await database.Database.MigrateAsync();
        if (!await database.Links.AnyAsync(link => link.Code == "EXPIRE1"))
        {
            database.Links.Add(new Link(
                "EXPIRE1",
                "https://example.test/expired",
                DateTimeOffset.UtcNow.AddDays(-2),
                DateTimeOffset.UtcNow.AddDays(-1)));
            await database.SaveChangesAsync();
        }
    }
}
