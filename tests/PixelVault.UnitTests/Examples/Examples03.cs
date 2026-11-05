using NUnit.Framework;
using PixelVault.UnitTests.Examples.TestObjects;

namespace PixelVault.UnitTests.Examples;

[TestFixture]
public class Examples03
{
    [Test]
    public void CheckThatDividingByZeroThrowsDivideByZeroException()
    {
        var calculator = new Calculator();

        calculator.Add(5);

        Assert.Throws<DivideByZeroException>(() =>
        {
            calculator.Divide(0);
        });
    }

    [Test]
    public void YouCanAlsoVerifyTheExceptionMessage()
    {
        var calculator = new Calculator();

        calculator.Add(5);

        var dbze = Assert.Throws<DivideByZeroException>(() =>
        {
            calculator.Divide(0);
        });

        Assert.That(dbze!.Message, Is.EqualTo("Attempted to divide by zero."));
    }
}