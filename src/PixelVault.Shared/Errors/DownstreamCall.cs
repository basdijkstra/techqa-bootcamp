using Polly.Timeout;

namespace PixelVault.Shared.Errors;

public static class DownstreamCall
{
    /// <summary>
    /// Sends a request to another PixelVault service. Connection failures and timeouts become a
    /// <see cref="DownstreamServiceException"/> (502) instead of an unhandled 500.
    /// </summary>
    public static async Task<HttpResponseMessage> SendAsync(string service, Func<Task<HttpResponseMessage>> send)
    {
        try
        {
            return await send();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException or TimeoutRejectedException)
        {
            throw new DownstreamServiceException(service, ex.Message);
        }
    }
}
