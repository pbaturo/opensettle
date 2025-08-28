using System;
using System.Globalization;
using FluentAssertions;
using OpenSettle.Money;
using Xunit;

namespace OpenSettle.Money.Tests;

public class MoneySpec
{
    [Fact]
    public void creates_and_normalizes_currency_and_rounds_to_two_decimals()
    {
        var m = new OpenSettle.Money.Money(12.345m, "pln");
        m.Amount.Should().Be(12.34m);
        m.Currency.Should().Be("PLN");
        m.ToString().Should().Be("12.34 PLN");
    }

    [Fact]
    public void rejects_negative_amount_and_non_three_letter_currency()
    {
        Action a1 = () => new OpenSettle.Money.Money(-0.01m, "PLN");
        Action a2 = () => new OpenSettle.Money.Money(10m, "PL");     // too short
        Action a3 = () => new OpenSettle.Money.Money(10m, "PL1");    // not letters
        a1.Should().Throw<ArgumentOutOfRangeException>();
        a2.Should().Throw<ArgumentException>();
        a3.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void arithmetic_only_with_same_currency_and_non_negative_result()
    {
        var a = new OpenSettle.Money.Money(100m, "PLN");
        var b = new OpenSettle.Money.Money(40m, "PLN");
        (a + b).Should().Be(new OpenSettle.Money.Money(140m, "PLN"));
        (a - b).Should().Be(new OpenSettle.Money.Money(60m, "PLN"));

        var eur = new OpenSettle.Money.Money(1m, "EUR");
        Action mix = () => { var _ = a + eur; };
        mix.Should().Throw<InvalidOperationException>();

        Action belowZero = () => { var _ = new OpenSettle.Money.Money(5m, "PLN") - new OpenSettle.Money.Money(6m, "PLN"); };
        belowZero.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void multiply_uses_bankers_rounding_to_two_decimals()
    {
        var a = new OpenSettle.Money.Money(10m, "PLN");
        (a * 1.235m).Amount.Should().Be(12.35m); // 12.35 ToEven
        (a * 1.225m).Amount.Should().Be(12.25m); // 12.25 ToEven
    }

    [Fact]
    public void percent_is_a_convenient_wrapper_over_multiplication()
    {
        var a = new OpenSettle.Money.Money(200m, "PLN");
        a.Percent(10).Should().Be(new OpenSettle.Money.Money(220m, "PLN"));
        a.Percent(-5).Should().Be(new OpenSettle.Money.Money(190m, "PLN"));
    }

    [Fact]
    public void tryparse_supports_both_token_orders_and_trims_whitespace()
    {
        OpenSettle.Money.Money.TryParse("12.5 PLN", out var m1).Should().BeTrue();
        m1.Should().Be(new OpenSettle.Money.Money(12.5m, "PLN"));

        OpenSettle.Money.Money.TryParse("  PLN   7.25  ", out var m2).Should().BeTrue();
        m2.Should().Be(new OpenSettle.Money.Money(7.25m, "PLN"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("PLN twelve")]
    [InlineData("123.45")]
    [InlineData("EUR 12.34.56")]
    public void tryparse_rejects_invalid_inputs(string input)
        => OpenSettle.Money.Money.TryParse(input, out _).Should().BeFalse();

    [Fact]
    public void parse_throws_on_invalid_input()
    {
        Action act = () => OpenSettle.Money.Money.Parse("foo bar");
        act.Should().Throw<FormatException>();
    }
}
