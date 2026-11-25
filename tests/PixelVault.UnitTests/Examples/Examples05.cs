using Moq;
using NUnit.Framework;
using PixelVault.UnitTests.Examples.TestObjects;

namespace PixelVault.UnitTests.Examples;

/*
 * The HeatAlert class in the TestObjects folder receives its dependencies through its constructor:
 * an IWeatherService that tells it the temperature, and an IMessageSender that sends the alert.
 * In these tests, we replace both dependencies with mocks created with Moq, so that:
 * - we decide what the weather service returns, without needing a real weather service
 * - we can check whether, and with which message, the message sender was called
 */
[TestFixture]
public class Examples05
{
    [Test]
    public async Task CheckAsync_TemperatureAboveThreshold_SendsHeatAlert()
    {
        // Create a mock for each dependency. A mock is a fake implementation of an interface.
        var weatherService = new Mock<IWeatherService>();
        var messageSender = new Mock<IMessageSender>();

        // Setup() and Returns(): when GetTemperature is called with "Amsterdam", return 32.
        weatherService
            .Setup(service => service.GetTemperature("Amsterdam"))
            .Returns(32);

        // SendAsync returns a Task<bool>, so we use ReturnsAsync() instead of Returns().
        // It.IsAny<T>() matches any argument value. All arguments must be specified, including the optional
        // CancellationToken: Moq does not allow you to leave out optional parameters.
        messageSender
            .Setup(sender => sender.SendAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Pass the mocked objects to the class under test using .Object.
        // A Mock<IWeatherService> is not an IWeatherService itself, its Object property is.
        var heatAlert = new HeatAlert(weatherService.Object, messageSender.Object);

        var alertSent = await heatAlert.CheckAsync("Amsterdam");

        // Check the result, like in any other test.
        Assert.That(alertSent, Is.True);

        // Verify(): check that SendAsync was called exactly once, with this exact message.
        messageSender.Verify(
            sender => sender.SendAsync("Heat alert for Amsterdam: 32 degrees", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task CheckAsync_TemperatureBelowThreshold_DoesNotSendHeatAlert()
    {
        var weatherService = new Mock<IWeatherService>();
        var messageSender = new Mock<IMessageSender>();

        // It.IsAny<string>(): return 18, whatever city is passed in.
        weatherService
            .Setup(service => service.GetTemperature(It.IsAny<string>()))
            .Returns(18);

        // The message sender does not need a Setup(): it should not be called at all in this test.

        var heatAlert = new HeatAlert(weatherService.Object, messageSender.Object);

        var alertSent = await heatAlert.CheckAsync("Amsterdam");

        Assert.That(alertSent, Is.False);

        // Verify() with Times.Never: check that SendAsync was not called, with any arguments.
        messageSender.Verify(
            sender => sender.SendAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
