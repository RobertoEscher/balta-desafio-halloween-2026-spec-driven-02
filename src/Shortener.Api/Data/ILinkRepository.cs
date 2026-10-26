using Shortener.Api.Domain;

namespace Shortener.Api.Data;

public interface ILinkRepository
{
    Task<bool> IsCodeTakenAsync(string code, CancellationToken cancellationToken);

    Task AddAsync(Link link, CancellationToken cancellationToken);

    Task<Link?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task<bool> IncrementAccessCountAsync(string code, CancellationToken cancellationToken);

    Task<long?> GetAccessCountAsync(string code, CancellationToken cancellationToken);
}
