using System.Globalization;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace AtomFinance.WinUI.Helpers;

public static class Formatters
{
    public static readonly SolidColorBrush MintBrush = new(Windows.UI.Color.FromArgb(255, 60, 227, 180));
    public static readonly SolidColorBrush MintBgBrush = new(Windows.UI.Color.FromArgb(45, 60, 227, 180));

    public static readonly SolidColorBrush PurpleBrush = new(Windows.UI.Color.FromArgb(255, 138, 124, 251));
    public static readonly SolidColorBrush PurpleBgBrush = new(Windows.UI.Color.FromArgb(45, 138, 124, 251));

    public static readonly SolidColorBrush RedBrush = new(Windows.UI.Color.FromArgb(255, 235, 87, 87));
    public static readonly SolidColorBrush RedBgBrush = new(Windows.UI.Color.FromArgb(45, 235, 87, 87));

    public static readonly SolidColorBrush YellowBrush = new(Windows.UI.Color.FromArgb(255, 242, 201, 76));
    public static readonly SolidColorBrush YellowBgBrush = new(Windows.UI.Color.FromArgb(45, 242, 201, 76));

    public static string Currency(decimal amount, string currency = "VND")
    {
        if (string.Equals(currency, "USD", StringComparison.OrdinalIgnoreCase))
            return amount.ToString("C2", CultureInfo.CreateSpecificCulture("en-US"));

        return $"{amount:N0} đ";
    }

    public static string CompactCurrency(decimal amount)
    {
        if (amount >= 1_000_000_000m)
            return $"{amount / 1_000_000_000m:0.##}B";
        if (amount >= 1_000_000m)
            return $"{amount / 1_000_000m:0.##}M";
        if (amount >= 1_000m)
            return $"{amount / 1_000m:0.##}K";
        return amount.ToString("N0");
    }

    public static string RelativeDate(DateTime dt)
    {
        var today = DateTime.Today;
        if (dt.Date == today)
            return $"Hôm nay, {dt:HH:mm}";
        if (dt.Date == today.AddDays(-1))
            return $"Hôm qua, {dt:HH:mm}";
        return dt.ToString("dd/MM/yyyy HH:mm");
    }

    public static bool TryParseCurrency(string? input, out decimal amount)
    {
        amount = 0m;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        string s = input.Trim().Replace(" ", "").Replace("đ", "").Replace("VND", "", StringComparison.OrdinalIgnoreCase);

        // If string contains both '.' and ',', whichever appears last is the decimal separator
        int lastDot = s.LastIndexOf('.');
        int lastComma = s.LastIndexOf(',');

        if (lastDot >= 0 && lastComma >= 0)
        {
            if (lastDot > lastComma)
            {
                s = s.Replace(",", "");
            }
            else
            {
                s = s.Replace(".", "").Replace(",", ".");
            }
        }
        else if (lastComma >= 0)
        {
            // Check if comma is a thousand separator (e.g., 50,000 or 1,000,000)
            int digitsAfter = s.Length - lastComma - 1;
            if (digitsAfter == 3 && s.Count(c => c == ',') >= 1)
                s = s.Replace(",", "");
            else
                s = s.Replace(",", ".");
        }
        else if (lastDot >= 0)
        {
            int digitsAfter = s.Length - lastDot - 1;
            if (digitsAfter == 3 && s.Count(c => c == '.') >= 1)
                s = s.Replace(".", "");
        }

        return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }
}
