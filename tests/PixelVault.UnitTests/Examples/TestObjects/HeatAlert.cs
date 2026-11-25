namespace PixelVault.UnitTests.Examples.TestObjects;

public interface IWeatherService
{
    /// <summary>Returns the current temperature in a city, in degrees Celsius.</summary>
    int GetTemperature(string city);
}

public interface IMessageSender
{
    /// <summary>Sends a message. Returns true when the message was delivered.</summary>
    Task<bool> SendAsync(string message, CancellationToken cancellationToken = default);
}

/// <summary>
/// Sends a heat alert when the temperature in a city is 30 degrees or more.
/// </summary>
public class HeatAlert
{
    public const int Threshold = 30;

    private readonly IWeatherService weatherService;
    private readonly IMessageSender messageSender;

    public HeatAlert(IWeatherService weatherService, IMessageSender messageSender)
    {
        this.weatherService = weatherService;
        this.messageSender = messageSender;
    }

    /// <summary>Returns true when an alert was sent and delivered, false otherwise.</summary>
    public async Task<bool> CheckAsync(string city, CancellationToken cancellationToken = default)
    {
        var temperature = weatherService.GetTemperature(city);

        if (temperature < Threshold)
        {
            return false;
        }

        return await messageSender.SendAsync($"Heat alert for {city}: {temperature} degrees", cancellationToken);
    }
}
