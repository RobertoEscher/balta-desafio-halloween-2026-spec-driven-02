using System.Collections.Concurrent;
using Shortener.Api.Data;
using Shortener.Api.Domain;
using Shortener.Api.Services;

namespace Shortener.Tests.Services;

public sealed class LinkServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 26, 23, 59, 59, TimeSpan.Zero);
    private const string ValidUrl = "https://example.test/destination";

    [Fact]
    public async Task CreateAsync_RetriesAfterAutomaticCodeCollision()
    {
        var repository = new InMemoryLinkRepository();
        repository.Seed(new Link("AAAAAAA", ValidUrl, Now, null));
        var service = CreateService(
            repository,
            new SequenceCodeGenerator("AAAAAAA", "BBBBBBB"),
            new FixedTimeProvider(Now));

        var result = await service.CreateAsync(new CreateLinkCommand(ValidUrl, null, null), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("BBBBBBB", result.Link!.Code);
    }

    [Fact]
    public async Task CreateAsync_ReturnsConflictForDuplicateCustomCode()
    {
        var repository = new InMemoryLinkRepository();
        repository.Seed(new Link("CUSTOM1", ValidUrl, Now, null));
        var service = CreateService(repository, new SequenceCodeGenerator(), new FixedTimeProvider(Now));

        var result = await service.CreateAsync(
            new CreateLinkCommand(ValidUrl, "CUSTOM1", null),
            CancellationToken.None);

        Assert.True(result.Conflict);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("lowerc1")]
    [InlineData("ABCDEF0")]
    [InlineData("TOOLONG1")]
    public async Task CreateAsync_RejectsInvalidCustomCode(string code)
    {
        var service = CreateService(
            new InMemoryLinkRepository(),
            new SequenceCodeGenerator(),
            new FixedTimeProvider(Now));

        var result = await service.CreateAsync(
            new CreateLinkCommand(ValidUrl, code, null),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("code", result.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_RejectsExpirationDateInPastAccordingToUtc()
    {
        var now = new DateTimeOffset(2026, 10, 26, 23, 30, 0, TimeSpan.FromHours(-2));
        var service = CreateService(
            new InMemoryLinkRepository(),
            new SequenceCodeGenerator("AAAAAAA"),
            new FixedTimeProvider(now));

        var result = await service.CreateAsync(
            new CreateLinkCommand(ValidUrl, null, "2026-10-26"),
            CancellationToken.None);

        Assert.Contains("expiresOnUtc", result.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_ExpiresAtStartOfNextUtcDay()
    {
        var service = CreateService(
            new InMemoryLinkRepository(),
            new SequenceCodeGenerator("AAAAAAA"),
            new FixedTimeProvider(Now));

        var result = await service.CreateAsync(
            new CreateLinkCommand(ValidUrl, null, "2026-10-26"),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        var expiry = new DateTimeOffset(2026, 10, 27, 0, 0, 0, TimeSpan.Zero);
        Assert.Equal(expiry, result.Link!.ExpiresAtUtc);
        Assert.False(result.Link.IsExpired(Now));
        Assert.True(result.Link.IsExpired(expiry));
    }

    [Fact]
    public async Task GetRedirectAsync_DoesNotIncrementExpiredLink()
    {
        var repository = new InMemoryLinkRepository();
        repository.Seed(new Link("EXPIRED", ValidUrl, Now.AddDays(-2), Now));
        var service = CreateService(
            repository,
            new SequenceCodeGenerator(),
            new FixedTimeProvider(Now));

        var result = await service.GetRedirectAsync("EXPIRED", CancellationToken.None);

        Assert.Equal(RedirectStatus.Expired, result.Status);
        Assert.Equal(0, await repository.GetAccessCountAsync("EXPIRED", CancellationToken.None));
    }

    private static LinkService CreateService(
        ILinkRepository repository,
        ILinkCodeGenerator generator,
        TimeProvider timeProvider) =>
        new(repository, generator, new AccessibleUrlValidator(), timeProvider);

    private sealed class SequenceCodeGenerator(params string[] codes) : ILinkCodeGenerator
    {
        private readonly Queue<string> _codes = new(codes);

        public string Generate() => _codes.Dequeue();
    }

    private sealed class AccessibleUrlValidator : IUrlValidator
    {
        public Task<string?> ValidateAsync(string? value, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(null);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class InMemoryLinkRepository : ILinkRepository
    {
        private readonly ConcurrentDictionary<string, Link> _links = new(StringComparer.Ordinal);

        public void Seed(Link link) => _links[link.Code] = link;

        public Task<bool> IsCodeTakenAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult(_links.ContainsKey(code));

        public Task AddAsync(Link link, CancellationToken cancellationToken)
        {
            if (!_links.TryAdd(link.Code, link))
            {
                throw new InvalidOperationException("Código duplicado no repositório de teste.");
            }

            return Task.CompletedTask;
        }

        public Task<Link?> GetByCodeAsync(string code, CancellationToken cancellationToken)
        {
            _links.TryGetValue(code, out var link);
            return Task.FromResult(link);
        }

        public Task<bool> IncrementAccessCountAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult(_links.ContainsKey(code));

        public Task<long?> GetAccessCountAsync(string code, CancellationToken cancellationToken)
        {
            var count = _links.TryGetValue(code, out var link) ? link.AccessCount : (long?)null;
            return Task.FromResult(count);
        }
    }
}
