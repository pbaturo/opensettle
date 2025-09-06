using Xunit;

namespace OpenSettle.Money.Tests;

public class MoneyTest
{
    [Fact]
    public void ValidMoneyRoundsAmountAndSetsCurrency()
    {
        // Arrange
        var amount = 10.123m;
        var currency = "USD";

        // Act
        var money = new Money(amount, currency);

        // Assert
        Assert.Equal(10.12m, money.Amount);
        Assert.Equal(currency, money.Currency);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(-0.01)]
    public void NegativeAmountThrowsArgumentOutOfRangeException(decimal amount)
    {
        // Arrange
        var currency = "EUR";

        // Act & Assert
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => new Money(amount, currency));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NullOrWhitespaceCurrencyThrowsArgumentException(string currency)
    {
        // Arrange
        var amount = 10m;

        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => new Money(amount, currency));
    }

    [Theory]
    [InlineData("USDD")]
    [InlineData("US")]
    public void InvalidCurrencyLengthThrowsArgumentException(string currency)
    {
        // Arrange
        var amount = 10m;

        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => new Money(amount, currency));
    }
}
