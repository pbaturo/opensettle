using System;
using OpenSettle.Money;
using Xunit;

namespace OpenSettle.MoneyTests;

public class MoneyTest
{
    [Fact]
    public void ValidMoney_ShouldRoundAmountAndSetCurrency()
    {
        // Arrange
        decimal amount = 10.123m;
        string currency = "USD";

        // Act
        var money = new OpenSettle.Money.Money(amount, currency);

        // Assert
        Assert.Equal(Math.Round(amount, 2, MidpointRounding.ToEven), money.Amount);
        Assert.Equal(currency, money.Currency);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(-0.01)]
    public void NegativeAmount_ShouldThrowArgumentOutOfRangeException(decimal amount)
    {
        // Arrange
        string currency = "EUR";

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new OpenSettle.Money.Money(amount, currency));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NullOrWhitespaceCurrency_ShouldThrowArgumentException(string currency)
    {
        // Arrange
        decimal amount = 10.0m;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new OpenSettle.Money.Money(amount, currency));
        Assert.Contains("Currency", exception.Message);
    }

    [Theory]
    [InlineData("US")]
    [InlineData("EURO")]
    public void InvalidCurrencyLength_ShouldThrowArgumentException(string currency)
    {
        // Arrange
        decimal amount = 10.0m;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new OpenSettle.Money.Money(amount, currency));
    }
}