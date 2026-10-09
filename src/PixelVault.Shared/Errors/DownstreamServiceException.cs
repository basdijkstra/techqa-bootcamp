namespace PixelVault.Shared.Errors;

/// <summary>
/// Thrown by HTTP clients when another PixelVault service is unreachable or returns an unexpected response.
/// </summary>
public class DownstreamServiceException : DomainException
{
    public DownstreamServiceException(string service, string message)
        : base("DOWNSTREAM_SERVICE_ERROR", $"{service} service error: {message}")
    {
        Service = service;
        Details["service"] = service;
    }

    public string Service { get; }

    public override int StatusCode => 502;
}
