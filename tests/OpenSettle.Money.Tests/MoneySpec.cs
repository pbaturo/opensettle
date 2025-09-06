using FluentAssertions;
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
        _ = money.Amount.Should().Be(12.34m);
        _ = money.Currency.Should().Be("PLN");
        _ = money.ToString().Should().Be("12.34 PLN");
    }

    [Fact]
    public void RejectsNegativeAmountAndNonThreeLetterCurrency()
    {
        // Arrange & Act & Assert
        Action action1 = () => new Money(-0.01m, "PLN");
        Action action2 = () => new Money(1m, "PŁN");
        Action action3 = () => new Money(10m, "PL");
        Action action4 = () => new Money(10m, "PL1");

        _ = action1.Should().Throw<ArgumentOutOfRangeException>();
        _ = action2.Should().Throw<ArgumentException>();
        _ = action3.Should().Throw<ArgumentException>();
        _ = action4.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ArithmeticOnlyWithSameCurrencyAndNonNegativeResult()
    {
        // Arrange
        Money a = new(100m, "PLN");
        Money b = new(40m, "PLN");
        Money eur = new(1m, "EUR");

        // Act & Assert
        _ = (a + b).Should().Be(new Money(140m, "PLN"));

        Action mixedCurrencies = () => { _ = a + eur; };
        _ = mixedCurrencies.Should().Throw<InvalidOperationException>();

        Action negativeResult = () => { _ = new Money(5m, "PLN") - new Money(6m, "PLN"); };
        _ = negativeResult.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MultiplyUsesBankersRoundingToTwoDecimals()
    {
        Money money = new(10m, "PLN");
        _ = (money * 1.235m).Amount.Should().Be(12.35m);
        _ = (money * 1.225m).Amount.Should().Be(12.25m);
    }

    [Fact]
    public void PercentIsAConvenientWrapperOverMultiplication()
    {
        Money money = new(200m, "PLN");
        _ = money.Percent(10).Should().Be(new Money(220m, "PLN"));
        _ = money.Percent(-5).Should().Be(new Money(190m, "PLN"));
    }

    [Fact]
    public void TryParseSupportsBothTokenOrdersAndTrimsWhitespace()
    {
        // Act & Assert
        _ = Money.TryParse("12.5 PLN", out Money m1).Should().BeTrue();
        _ = m1.Should().Be(new Money(12.5m, "PLN"));

        _ = Money.TryParse("  PLN   7.25  ", out Money m2).Should().BeTrue();
        _ = m2.Should().Be(new Money(7.25m, "PLN"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("PLN twelve")]
    [InlineData("123.45")]
    [InlineData("EUR 12.34.56")]
    public void TryParseRejectsInvalidInputs(string input)
    {
        _ = Money.TryParse(input, out _).Should().BeFalse();
    }

    [Fact]
    public void ParseThrowsOnInvalidInput()
    {
        Action act = () => Money.Parse("foo bar");
        _ = act.Should().Throw<FormatException>();
    }
}
