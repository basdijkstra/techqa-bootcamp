using NUnit.Framework;

namespace PixelVault.UnitTests.Examples.TestObjects;

[TestFixture]
public class Examples01
{
    [Test]
    public void GivenANewCalculator_WhenIAdd2and2_thenTheTotalIs4()
    {
        // Given - Arrange
        var calculator = new Calculator();

        // When - Act
        calculator.Add(2);
        calculator.Add(2);

        // Then - Assert
        Assert.That(calculator.Total, Is.EqualTo(4));
    }

    [Test]
    public void GivenANewCalculator_WhenISubtract3From5_thenTheResultIs2()
    {
        // Given - Arrange
        var calculator = new Calculator();

        // When - Act
        calculator.Add(5);
        calculator.Subtract(3);

        // Then - Assert
        Assert.That(calculator.Total, Is.EqualTo(2));
    }
}