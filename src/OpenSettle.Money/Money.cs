
using System.Globalization;
namespace OpenSettle.Money;

public record struct Money
{
    public decimal Amount { get; }
    public string Currency { get; } = "";

    public Money(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be null or whitespace.", nameof(currency));
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

        currency = currency.Trim();

        if (currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO code.", nameof(currency));

        if (!IsAsciiLetters(currency))
            throw new ArgumentException("Currency must contain only letters A-Z.", nameof(currency));

        Amount = decimal.Round(amount, 2, MidpointRounding.ToEven);
        Currency = currency.ToUpperInvariant();
    }

    public static Money Zero(string currency) => new(0m, currency);

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Cannot add amounts with different currencies.");

        return new Money(a.Amount + b.Amount, a.Currency);
    }

    public static Money operator -(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Cannot subtract amounts with different currencies.");
        if (a.Amount < b.Amount)
            throw new InvalidOperationException("Resulting amount cannot be negative.");

        return new Money(a.Amount - b.Amount, a.Currency);
    }

    public static Money operator *(Money money, decimal factor)
    {
        if (factor < 0)
            throw new ArgumentOutOfRangeException(nameof(factor), "Factor cannot be negative.");

        return new Money(money.Amount * factor, money.Currency);
    }

    public Money Percent(decimal percent)
    {
        var amount = Amount * (1 + percent / 100m);
        return new Money(amount, Currency);
    }

      public override string ToString() =>
        string.Format(CultureInfo.InvariantCulture, "{0:0.00} {1}", Amount, Currency);
    public static bool TryParse(string? s, out Money money)
    {
        money = default;
        if (string.IsNullOrWhiteSpace(s)) return false;

        var parts = s.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) return false;

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
            return false;

        try { money = new Money(amount, curr); return true; }
        catch { return false; }
    }

    public static Money Parse(string s)
    {
        if (TryParse(s, out var money))
            return money;

        throw new FormatException("Input string was not in a correct format.");
    }
    
    private static bool IsAsciiLetters(string s)
    {
        foreach(char ch in s)
        {
            if (!((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z')))
                return false;
        }
        return true;
    }
}
