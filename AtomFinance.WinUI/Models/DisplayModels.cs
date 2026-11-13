using Microsoft.UI.Xaml.Media;
using PersonalFinance.Models;

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

public record DailyActivityItem(
    string DayName,
    decimal Amount,
    string AmountText,
    double BarHeight
);

public record NotificationItem(
    string Title,
    string Message,
    string TimeText,
    string Glyph,
    Brush AccentBrush,
    Brush BadgeBgBrush
);
