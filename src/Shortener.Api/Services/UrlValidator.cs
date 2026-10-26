namespace Shortener.Api.Services;

public sealed class UrlValidator(HttpClient httpClient) : IUrlValidator
{
    public async Task<string?> ValidateAsync(string? value, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "A URL de destino é obrigatória.";
        }

        if (value.Length > 2048)
        {
            return "A URL de destino deve ter no máximo 2048 caracteres.";
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || string.IsNullOrWhiteSpace(uri.Host)
            || !string.IsNullOrEmpty(uri.UserInfo))
        {
            return "Informe uma URL HTTP ou HTTPS válida.";
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, uri);
            using var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            return response.IsSuccessStatusCode
                ? null
                : "O destino não está acessível.";
        }
        catch (HttpRequestException)
        {
            return "O destino não está acessível.";
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return "O destino não está acessível.";
        }
    }
}
