using Microsoft.EntityFrameworkCore;
using Shortener.Api.Domain;

namespace Shortener.Api.Data;

public sealed class LinkRepository(LinkDbContext database) : ILinkRepository
{
    public Task<bool> IsCodeTakenAsync(string code, CancellationToken cancellationToken) =>
        database.Links.AnyAsync(link => link.Code == code, cancellationToken);

    public async Task AddAsync(Link link, CancellationToken cancellationToken)
    {
        database.Links.Add(link);
        await database.SaveChangesAsync(cancellationToken);
    }

    public Task<Link?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        database.Links.AsNoTracking().SingleOrDefaultAsync(link => link.Code == code, cancellationToken);

    public async Task<bool> IncrementAccessCountAsync(string code, CancellationToken cancellationToken)
    {
        var affectedRows = await database.Links
            .Where(link => link.Code == code)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(link => link.AccessCount, link => link.AccessCount + 1),
                cancellationToken);

        return affectedRows > 0;
    }

    public async Task<long?> GetAccessCountAsync(string code, CancellationToken cancellationToken) =>
        await database.Links
            .Where(link => link.Code == code)
            .Select(link => (long?)link.AccessCount)
            .SingleOrDefaultAsync(cancellationToken);
}
