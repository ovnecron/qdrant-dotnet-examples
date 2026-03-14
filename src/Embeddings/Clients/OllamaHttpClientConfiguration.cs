using System.Net.Http.Headers;

namespace Embeddings.Clients;

public static class OllamaHttpClientConfiguration
{
    public const string DefaultBaseUrl = "http://localhost:11434/api";

    public static void ConfigureJsonApiClient(
        HttpClient httpClient,
        string? baseUrl,
        string? apiKey)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        httpClient.BaseAddress = ResolveBaseAddress(baseUrl);
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            httpClient.DefaultRequestHeaders.Authorization = null;
            return;
        }

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            apiKey.Trim());
    }

    public static Uri ResolveBaseAddress(string? baseUrl)
    {
        var rawBaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl.Trim();

        if (!Uri.TryCreate(rawBaseUrl, UriKind.Absolute, out var parsedUri))
        {
            throw new InvalidOperationException("BaseUrl must be an absolute http/https URI.");
        }

        if (parsedUri.Scheme != Uri.UriSchemeHttp &&
            parsedUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("BaseUrl must use http or https.");
        }

        var builder = new UriBuilder(parsedUri);
        if (string.IsNullOrWhiteSpace(builder.Path) || builder.Path == "/")
        {
            builder.Path = "/api/";
            return builder.Uri;
        }

        if (!builder.Path.EndsWith("/", StringComparison.Ordinal))
        {
            builder.Path += "/";
        }

        return builder.Uri;
    }
}
