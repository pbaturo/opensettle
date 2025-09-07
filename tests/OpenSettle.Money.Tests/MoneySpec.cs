using Xunit;

namespace OpenSettle.Money.Tests;

public class MoneySpec
{
    [Fact]
    public void CreatesAndNormalizesCurrencyAndRoundsToTwoDecimals()
    {
        // Arrange & Act
        Money money = new(12.345m, "pln");

        // Assert
        Assert.Equal(12.34m, money.Amount);
        Assert.Equal("PLN", money.Currency);
        Assert.Equal("12.34 PLN", money.ToString());
    }

    [Fact]
    public void RejectsNegativeAmountAndNonThreeLetterCurrency()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(-0.01m, "PLN"));
        Assert.Throws<ArgumentException>(() => new Money(1m, "PŁN"));
        Assert.Throws<ArgumentException>(() => new Money(10m, "PL"));
        Assert.Throws<ArgumentException>(() => new Money(10m, "PL1"));
    }

    [Fact]
    public void ArithmeticOnlyWithSameCurrencyAndNonNegativeResult()
    {
        // Arrange
        Money a = new(100m, "PLN");
        Money b = new(40m, "PLN");
        Money eur = new(1m, "EUR");

        // Act & Assert
        Money sum = a + b;
        Assert.Equal(new Money(140m, "PLN"), sum);

        Assert.Throws<InvalidOperationException>(() => a + eur);
        Assert.Throws<InvalidOperationException>(() => new Money(5m, "PLN") - new Money(6m, "PLN"));
    }

    [Fact]
    public void MultiplyUsesBankersRoundingToTwoDecimals()
    {
        Money money = new(10m, "PLN");
        Assert.Equal(12.35m, (money * 1.235m).Amount);
        Assert.Equal(12.25m, (money * 1.225m).Amount);
    }

    [Fact]
    public void PercentIsAConvenientWrapperOverMultiplication()
    {
        Money money = new(200m, "PLN");
        Assert.Equal(new Money(220m, "PLN"), money.Percent(10));
        Assert.Equal(new Money(190m, "PLN"), money.Percent(-5));
    }

    [Fact]
    public void TryParseSupportsBothTokenOrdersAndTrimsWhitespace()
    {
        // Act & Assert
        bool result1 = Money.TryParse("12.5 PLN", out Money m1);
        Assert.True(result1);
        Assert.Equal(new Money(12.5m, "PLN"), m1);

        bool result2 = Money.TryParse("  PLN   7.25  ", out Money m2);
        Assert.True(result2);
        Assert.Equal(new Money(7.25m, "PLN"), m2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("PLN twelve")]
    [InlineData("123.45")]
    [InlineData("EUR 12.34.56")]
    public void TryParseRejectsInvalidInputs(string input)
    {
        bool result = Money.TryParse(input, out _);
        Assert.False(result);
    }

    [Fact]
    public void ParseThrowsOnInvalidInput()
    {
        Assert.Throws<FormatException>(() => Money.Parse("foo bar"));
    }
}
