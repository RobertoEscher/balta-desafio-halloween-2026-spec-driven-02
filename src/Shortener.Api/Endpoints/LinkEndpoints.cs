using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Shortener.Api.Services;

namespace Shortener.Api.Endpoints;

/// <summary>Payload used to create a shortened link.</summary>
public sealed record CreateLinkRequest
{
    /// <summary>The absolute HTTP or HTTPS destination URL.</summary>
    public string? TargetUrl { get; init; }

    /// <summary>An optional seven-character custom code.</summary>
    public string? Code { get; init; }

    /// <summary>An optional expiration calendar date in UTC using YYYY-MM-DD.</summary>
    public string? ExpiresOnUtc { get; init; }
}

/// <summary>Short link information returned after creation.</summary>
/// <param name="ShortUrl">The absolute shortened URL.</param>
/// <param name="Code">The seven-character short code.</param>
public sealed record CreateLinkResponse(string ShortUrl, string Code);

/// <summary>Public access statistics for a short link.</summary>
/// <param name="Code">The short code.</param>
/// <param name="AccessCount">The total number of redirect requests, including automated clients.</param>
public sealed record LinkStatsResponse(string Code, long AccessCount);

public static class LinkEndpoints
{
    public static WebApplication MapLinkEndpoints(this WebApplication app)
    {
        app.MapPost(
                "/api/links",
                async Task<Results<Created<CreateLinkResponse>, ValidationProblem, Conflict<ProblemDetails>>> (
                    CreateLinkRequest request,
                    LinkService service,
                    HttpContext context,
                    CancellationToken cancellationToken) =>
                {
                    var result = await service.CreateAsync(
                        new CreateLinkCommand(request.TargetUrl, request.Code, request.ExpiresOnUtc),
                        cancellationToken);

                    if (result.Conflict)
                    {
                        return TypedResults.Conflict(new ProblemDetails
                        {
                            Status = StatusCodes.Status409Conflict,
                            Title = "Conflito",
                            Detail = "O código não está disponível. Informe outro código ou tente novamente."
                        });
                    }

                    if (!result.Succeeded)
                    {
                        return TypedResults.ValidationProblem(result.Errors);
                    }

                    var link = result.Link!;
                    var response = new CreateLinkResponse(
                        $"{context.Request.Scheme}://{context.Request.Host}/{link.Code}",
                        link.Code);
                    return TypedResults.Created($"/{link.Code}", response);
                })
            .WithName("CreateLink")
            .WithSummary("Cria um link curto")
            .WithDescription("Valida o destino e cria um código curto automático ou personalizado.")
            .Produces<CreateLinkResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        app.MapGet(
                "/{code}",
                async Task<Results<RedirectHttpResult, NotFound, StatusCodeHttpResult>> (
                    string code,
                    LinkService service,
                    CancellationToken cancellationToken) =>
                {
                    var result = await service.GetRedirectAsync(code, cancellationToken);
                    return result.Status switch
                    {
                        RedirectStatus.Redirect => TypedResults.Redirect(result.TargetUrl!),
                        RedirectStatus.Expired => TypedResults.StatusCode(StatusCodes.Status410Gone),
                        _ => TypedResults.NotFound()
                    };
                })
            .WithName("RedirectLink")
            .WithSummary("Redireciona um código curto")
            .WithDescription("Redireciona links ativos e informa quando o código não existe ou expirou.")
            .Produces(StatusCodes.Status302Found)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status410Gone);

        app.MapGet(
                "/api/links/{code}/stats",
                async Task<Results<Ok<LinkStatsResponse>, NotFound>> (
                    string code,
                    LinkService service,
                    CancellationToken cancellationToken) =>
                {
                    var accessCount = await service.GetAccessCountAsync(code, cancellationToken);
                    return accessCount is null
                        ? TypedResults.NotFound()
                        : TypedResults.Ok(new LinkStatsResponse(code, accessCount.Value));
                })
            .WithName("GetLinkStats")
            .WithSummary("Consulta estatísticas públicas")
            .WithDescription("Retorna o total de acessos do link identificado pelo código.")
            .Produces<LinkStatsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}
