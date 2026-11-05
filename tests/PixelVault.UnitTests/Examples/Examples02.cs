using NUnit.Framework;
using PixelVault.UnitTests.Examples.TestObjects;

namespace PixelVault.UnitTests.Examples;

[TestFixture]
public class Examples02
{
    [TestCase(2, 2, 4, TestName = "2 and 2 is 4")]
    [TestCase(100, 99, 199, TestName = "100 and 99 is 199")]
    [TestCase(987, 13, 1000, TestName = "987 and 13 is 1000")]
    public void GivenANewCalculator_WhenIAddTwoNumbers_thenTheTotalIsAsExpected
        (int first, int second, int expectedTotal)
    {
        var calculator = new Calculator();

        calculator.Add(first);
        calculator.Add(second);

        Assert.That(calculator.Total, Is.EqualTo(expectedTotal));
    }

    [Test, TestCaseSource(nameof(CalculatorAdditionTestData))]
    public void GivenANewCalculator_WhenIAddTwoNumbers_thenTheTotalIsAsExpected_UsingTestCaseSource
        (int first, int second, int expectedTotal)
    {
        var calculator = new Calculator();

        calculator.Add(first);
        calculator.Add(second);

        Assert.That(calculator.Total, Is.EqualTo(expectedTotal));
    }

    private static IEnumerable<TestCaseData> CalculatorAdditionTestData()
    {
        yield return new TestCaseData(2, 2, 4).
            SetName("2 and 2 equals 4");
        yield return new TestCaseData(100, 99, 199).
            SetName("100 and 99 equals 199");
        yield return new TestCaseData(987, 13, 1000).
            SetName("987 and 13 equals 1000");
    }
}