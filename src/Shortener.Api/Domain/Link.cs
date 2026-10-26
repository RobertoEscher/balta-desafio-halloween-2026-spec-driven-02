namespace Shortener.Api.Domain;

public sealed class Link
{
    private Link()
    {
    }

    public Link(
        string code,
        string targetUrl,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? expiresAtUtc)
    {
        Code = code;
        TargetUrl = targetUrl;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public int Id { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string TargetUrl { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? ExpiresAtUtc { get; private set; }

    public long AccessCount { get; private set; }

    public bool IsExpired(DateTimeOffset nowUtc) =>
        ExpiresAtUtc is not null && nowUtc.ToUniversalTime() >= ExpiresAtUtc.Value;
}
