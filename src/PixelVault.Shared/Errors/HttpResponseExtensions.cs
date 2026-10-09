using System.Net.Http.Json;
using System.Text.Json;

namespace PixelVault.Shared.Errors;

public static class HttpResponseExtensions
{
    /// <summary>
    /// Throws when the response is not successful. A problem details body with a <c>code</c> from another
    /// PixelVault service is re-thrown with the same status code and error code, so that for example an
    /// <c>INSUFFICIENT_STOCK</c> error from Catalog reaches the browser unchanged. Anything else becomes a 502.
    /// </summary>
    public static async Task EnsureSuccessOrThrowAsync(this HttpResponseMessage response, string service, CancellationToken cancellationToken = default)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var status = (int)response.StatusCode;
        JsonElement? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        }
        catch (Exception)
        {
            // Not JSON, handled below.
        }

        if (status is >= 400 and < 500 && problem is { ValueKind: JsonValueKind.Object } body
            && body.TryGetProperty("code", out var codeElement))
        {
            var detail = body.TryGetProperty("detail", out var detailElement) ? detailElement.GetString() : null;
            var exception = new DomainStatusException(status, codeElement.GetString() ?? "UNKNOWN", detail ?? $"{service} rejected the request.");
            foreach (var property in body.EnumerateObject())
            {
                if (property.Name is "type" or "title" or "status" or "detail" or "code" or "traceId" or "instance")
                {
                    continue;
                }

                exception.Details[property.Name] = property.Value.Clone();
            }

            exception.Details["source"] = service;
            throw exception;
        }

        throw new DownstreamServiceException(service, $"unexpected response {status} {response.ReasonPhrase}");
    }
}
