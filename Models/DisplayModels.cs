using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Media;
using PersonalFinance.Models;
using Windows.UI.Text;

namespace AtomFinance.WinUI.Models;

public record TransactionItem(
    string Id,
    string Title,
    string Subtitle,
    string AmountText,
    Brush AmountBrush,
    Brush BadgeBgBrush,
    string Glyph,
    Transaction RawTransaction
);

public record AccountItem(
    string Id,
    string Name,
    string BalanceText,
    string Description,
    string TypeName,
    string MaskedNumber,
    int StyleIndex,
    Account RawAccount
);

public partial class DailyActivityItem : ObservableObject
{
    public string DayName { get; }
    public string FullDateLabel { get; }
    public decimal Amount { get; }
    public string AmountText { get; }
    public double NormalizedValue { get; }
    public bool IsToday { get; }
    public FontWeight DayFontWeight => IsToday ? FontWeights.Bold : FontWeights.Normal;

    [ObservableProperty]
    public partial double BarHeight { get; set; }

    [ObservableProperty]
    public partial double BarOpacity { get; set; }

    public DailyActivityItem(
        string dayName,
        string fullDateLabel,
        decimal amount,
        string amountText,
        double normalizedValue,
        double barHeight,
        double barOpacity,
        bool isToday)
    {
        DayName = dayName;
        FullDateLabel = fullDateLabel;
        Amount = amount;
        AmountText = amountText;
        NormalizedValue = normalizedValue;
        BarHeight = barHeight;
        BarOpacity = barOpacity;
        IsToday = isToday;
    }
}

public record NotificationItem(
    string Title,
    string Message,
    string TimeText,
    string Glyph,
    Brush AccentBrush,
    Brush BadgeBgBrush
);
