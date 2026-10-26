using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shortener.Api.Data;
using Shortener.Api.Domain;

namespace Shortener.Api.Services;

public sealed record CreateLinkCommand(string? TargetUrl, string? Code, string? ExpiresOnUtc);

public sealed record LinkCreationResult(
    Link? Link,
    IReadOnlyDictionary<string, string[]> Errors,
    bool Conflict)
{
    public bool Succeeded => Link is not null;
}

public enum RedirectStatus
{
    Redirect,
    NotFound,
    Expired
}

public sealed record LinkRedirectResult(RedirectStatus Status, string? TargetUrl);

public sealed class LinkService(
    ILinkRepository repository,
    ILinkCodeGenerator codeGenerator,
    IUrlValidator urlValidator,
    TimeProvider timeProvider)
{
    private const int AutomaticCodeAttempts = 10;

    public async Task<LinkCreationResult> CreateAsync(
        CreateLinkCommand command,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        var urlError = await urlValidator.ValidateAsync(command.TargetUrl, cancellationToken);
        if (urlError is not null)
        {
            errors["targetUrl"] = [urlError];
        }

        if (command.Code is not null && !IsValidCode(command.Code))
        {
            errors["code"] = ["O código deve conter exatamente 7 caracteres: letras maiúsculas de A a Z e dígitos de 1 a 9."];
        }

        if (!TryGetExpiration(command.ExpiresOnUtc, out var expiresAtUtc, out var expirationError))
        {
            errors["expiresOnUtc"] = [expirationError!];
        }

        if (errors.Count > 0)
        {
            return new LinkCreationResult(null, errors, Conflict: false);
        }

        if (command.Code is not null && await repository.IsCodeTakenAsync(command.Code, cancellationToken))
        {
            return ConflictResult();
        }

        var attempts = command.Code is null ? AutomaticCodeAttempts : 1;
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            var code = command.Code ?? codeGenerator.Generate();
            if (!IsValidCode(code))
            {
                throw new InvalidOperationException("O gerador produziu um código fora do formato permitido.");
            }

            if (await repository.IsCodeTakenAsync(code, cancellationToken))
            {
                if (command.Code is not null)
                {
                    return ConflictResult();
                }

                continue;
            }

            var link = new Link(
                code,
                command.TargetUrl!,
                timeProvider.GetUtcNow().ToUniversalTime(),
                expiresAtUtc);

            try
            {
                await repository.AddAsync(link, cancellationToken);
                return new LinkCreationResult(link, new Dictionary<string, string[]>(), Conflict: false);
            }
            catch (DbUpdateException exception) when (IsUniqueCodeViolation(exception))
            {
                if (command.Code is not null)
                {
                    return ConflictResult();
                }
            }
        }

        return ConflictResult();
    }

    public async Task<LinkRedirectResult> GetRedirectAsync(
        string code,
        CancellationToken cancellationToken)
    {
        var link = await repository.GetByCodeAsync(code, cancellationToken);
        if (link is null)
        {
            return new LinkRedirectResult(RedirectStatus.NotFound, null);
        }

        if (link.IsExpired(timeProvider.GetUtcNow()))
        {
            return new LinkRedirectResult(RedirectStatus.Expired, null);
        }

        await repository.IncrementAccessCountAsync(code, cancellationToken);
        return new LinkRedirectResult(RedirectStatus.Redirect, link.TargetUrl);
    }

    public Task<long?> GetAccessCountAsync(string code, CancellationToken cancellationToken) =>
        repository.GetAccessCountAsync(code, cancellationToken);

    public static bool IsValidCode(string code) =>
        code.Length == LinkCodeGenerator.CodeLength
        && code.All(character => LinkCodeGenerator.Alphabet.Contains(character));

    private bool TryGetExpiration(
        string? value,
        out DateTimeOffset? expiresAtUtc,
        out string? error)
    {
        expiresAtUtc = null;
        error = null;
        if (value is null)
        {
            return true;
        }

        if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            error = "A data de expiração deve usar o formato YYYY-MM-DD.";
            return false;
        }

        var todayUtc = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        if (date < todayUtc)
        {
            error = "A data de expiração não pode estar no passado.";
            return false;
        }

        try
        {
            expiresAtUtc = new DateTimeOffset(date.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            error = "A data de expiração está fora do intervalo permitido.";
            return false;
        }
    }

    private static bool IsUniqueCodeViolation(DbUpdateException exception) =>
        exception.InnerException is SqliteException
        {
            SqliteExtendedErrorCode: 1555 or 2067
        };

    private static LinkCreationResult ConflictResult() =>
        new(
            null,
            new Dictionary<string, string[]>(),
            Conflict: true);
}
