using System.Net;
using Shortener.Api.Services;

namespace Shortener.Tests.Services;

public sealed class UrlValidatorTests
{
    [Theory]
    [InlineData("http://example.test/")]
    [InlineData("https://example.test/path")]
    public async Task ValidateAsync_AcceptsAccessibleHttpAndHttps(string value)
    {
        using var client = CreateClient(HttpStatusCode.OK);
        var validator = new UrlValidator(client);

        var error = await validator.ValidateAsync(value, CancellationToken.None);

        Assert.Null(error);
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("ftp://example.test/file")]
    public async Task ValidateAsync_RejectsInvalidOrUnsupportedUrls(string value)
    {
        using var client = CreateClient(HttpStatusCode.OK);
        var validator = new UrlValidator(client);

        var error = await validator.ValidateAsync(value, CancellationToken.None);

        Assert.NotNull(error);
    }

    [Fact]
    public async Task ValidateAsync_RejectsUrlsLongerThan2048Characters()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(handler);
        var validator = new UrlValidator(client);
        var value = $"https://example.test/{new string('a', 2048)}";

        var error = await validator.ValidateAsync(value, CancellationToken.None);

        Assert.NotNull(error);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task ValidateAsync_RejectsInaccessibleDestination()
    {
        using var client = CreateClient(HttpStatusCode.NotFound);
        var validator = new UrlValidator(client);

        var error = await validator.ValidateAsync("https://example.test/missing", CancellationToken.None);

        Assert.Equal("O destino não está acessível.", error);
    }

    [Fact]
    public async Task ValidateAsync_RejectsConnectionFailure()
    {
        using var client = new HttpClient(new StubHandler(_ => throw new HttpRequestException()));
        var validator = new UrlValidator(client);

        var error = await validator.ValidateAsync("https://example.test/", CancellationToken.None);

        Assert.Equal("O destino não está acessível.", error);
    }

    private static HttpClient CreateClient(HttpStatusCode statusCode) =>
        new(new StubHandler(_ => new HttpResponseMessage(statusCode)));

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(responseFactory(request));
        }
    }
}
