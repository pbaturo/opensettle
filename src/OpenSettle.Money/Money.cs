using System.Globalization;
namespace OpenSettle.Money;

/// <summary>
/// Represents a monetary amount with its currency.
/// </summary>
public readonly record struct Money
{
    /// <summary>
    /// Gets the monetary amount.
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// Gets the currency code (3-letter ISO code).
    /// </summary>
    public string Currency { get; }

    /// <summary>
    /// Initializes a new instance of the Money struct.
    /// </summary>
    /// <param name="amount">The monetary amount (must be non-negative).</param>
    /// <param name="currency">The currency code (3-letter ISO code).</param>
    /// <exception cref="ArgumentException">Thrown when currency is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when amount is negative.</exception>
    public Money(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency cannot be null or whitespace.", nameof(currency));
        }
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");
        }

        currency = currency.Trim();
        if (currency.Length != 3)
        {
            throw new ArgumentException("Currency must be a 3-letter ISO code.", nameof(currency));
        }
        if (!IsAsciiLetters(currency))
        {
            throw new ArgumentException("Currency must contain only letters A-Z.", nameof(currency));
        }
        Amount = decimal.Round(amount, 2, MidpointRounding.ToEven);
        Currency = currency.ToUpperInvariant();
    }

    /// <summary>
    /// Creates a Money instance with zero amount for the specified currency.
    /// </summary>
    /// <param name="currency">The currency code.</param>
    /// <returns>A Money instance with zero amount.</returns>
    public static Money Zero(string currency)
    {
        return new(0m, currency);
    }

    /// <summary>
    /// Adds two Money instances with the same currency.
    /// </summary>
    /// <param name="a">The first Money instance.</param>
    /// <param name="b">The second Money instance.</param>
    /// <returns>The sum of the two amounts.</returns>
    /// <exception cref="InvalidOperationException">Thrown when currencies differ.</exception>
    public static Money operator +(Money a, Money b)
    {
        return a.Currency != b.Currency
            ? throw new InvalidOperationException("Cannot add amounts with different currencies.")
            : new Money(a.Amount + b.Amount, a.Currency);
    }

    /// <summary>
    /// Subtracts two Money instances with the same currency.
    /// </summary>
    /// <param name="a">The first Money instance.</param>
    /// <param name="b">The second Money instance.</param>
    /// <returns>The difference of the two amounts.</returns>
    /// <exception cref="InvalidOperationException">Thrown when currencies differ or result would be negative.</exception>
    public static Money operator -(Money a, Money b)
    {
        return a.Currency != b.Currency
            ? throw new InvalidOperationException("Cannot subtract amounts with different currencies.")
            : a.Amount < b.Amount
            ? throw new InvalidOperationException("Resulting amount cannot be negative.")
            : new Money(a.Amount - b.Amount, a.Currency);
    }

    /// <summary>
    /// Multiplies a Money instance by a decimal factor.
    /// </summary>
    /// <param name="money">The Money instance.</param>
    /// <param name="factor">The multiplication factor (must be non-negative).</param>
    /// <returns>The Money instance multiplied by the factor.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when factor is negative.</exception>
    public static Money operator *(Money money, decimal factor)
    {
        return factor < 0
            ? throw new ArgumentOutOfRangeException(nameof(factor), "Factor cannot be negative.")
            : new Money(money.Amount * factor, money.Currency);
    }

    /// <summary>
    /// Calculates the amount with a percentage increase or decrease.
    /// </summary>
    /// <param name="percent">The percentage to apply (positive for increase, negative for decrease).</param>
    /// <returns>A new Money instance with the percentage applied.</returns>
    public Money Percent(decimal percent)
    {
        var amount = Amount * (1 + (percent / 100m));
        return new Money(amount, Currency);
    }

    /// <summary>
    /// Returns a string representation of the Money instance.
    /// </summary>
    /// <returns>A string in the format "amount currency" (e.g., "12.34 USD").</returns>
    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture, "{0:0.00} {1}", Amount, Currency);
    }

    /// <summary>
    /// Tries to parse a string representation of Money.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="money">When successful, contains the parsed Money instance.</param>
    /// <returns>True if parsing was successful; otherwise, false.</returns>
    public static bool TryParse(string? s, out Money money)
    {
        money = default;
        if (string.IsNullOrWhiteSpace(s))
        {
            return false;
        }

        var parts = s.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        string curr, num;
        if (parts[0].Length == 3 && IsAsciiLetters(parts[0]))
        {
            // "PLN 12.50"
            curr = parts[0];
            num = parts[1];
        }
        else
        {
            // "12.50 PLN"
            curr = parts[1];
            num = parts[0];
        }

        if (!decimal.TryParse(num, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            return false;
        }

        try { money = new Money(amount, curr); return true; }
        catch { return false; }
    }

    /// <summary>
    /// Parses a string representation of Money.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <returns>The parsed Money instance.</returns>
    /// <exception cref="FormatException">Thrown when the string format is invalid.</exception>
    public static Money Parse(string s)
    {
        return TryParse(s, out Money money) ? money : throw new FormatException("Input string was not in a correct format.");
    }

    /// <summary>
    /// Determines whether a string contains only ASCII letters (A-Z, a-z).
    /// </summary>
    /// <param name="s">The string to check.</param>
    /// <returns>True if the string contains only ASCII letters; otherwise, false.</returns>
    private static bool IsAsciiLetters(string s)
    {
        foreach (var ch in s)
        {
            if (ch is not ((>= 'A' and <= 'Z') or (>= 'a' and <= 'z')))
            {
                return false;
            }
        }
        return true;
    }
}
