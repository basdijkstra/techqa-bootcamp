namespace PixelVault.UnitTests.Examples.TestObjects;

public class Calculator
{
    public int Total { get; private set; }

    public Calculator()
    {
        Total = 0;
    }

    public void Add(int valueToAdd)
    {
        Total += valueToAdd;
    }

    public void Subtract(int valueToSubstract)
    {
        Total -= valueToSubstract;
    }

    public void Multiply(int valueToMultiplyWith)
    {
        Total *= valueToMultiplyWith;
    }

    public void Divide(int valueToDivideBy)
    {
        Total /= valueToDivideBy;
    }
}