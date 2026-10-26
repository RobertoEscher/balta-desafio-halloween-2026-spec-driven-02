using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shortener.Api.Data;
using Shortener.Api.Domain;

namespace Shortener.Tests.Data;

public sealed class LinkRepositoryTests
{
    [Fact]
    public async Task IncrementAccessCountAsync_NeverDecreasesTheCounter()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<LinkDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var database = new LinkDbContext(options))
        {
            await database.Database.MigrateAsync();
            database.Links.Add(new Link(
                "COUNTER",
                "https://example.test/",
                DateTimeOffset.UtcNow,
                null));
            await database.SaveChangesAsync();

            var repository = new LinkRepository(database);
            Assert.True(await repository.IncrementAccessCountAsync("COUNTER", CancellationToken.None));
            Assert.True(await repository.IncrementAccessCountAsync("COUNTER", CancellationToken.None));
            Assert.Equal(2L, await repository.GetAccessCountAsync("COUNTER", CancellationToken.None));
        }
    }
}
