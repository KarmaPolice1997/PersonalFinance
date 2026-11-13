using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using AtomFinance.WinUI.Helpers;
using AtomFinance.WinUI.Models;
using PersonalFinance.Models;
using PersonalFinance.Services;

namespace AtomFinance.WinUI.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private const string RightPanelWidthKey = "Dashboard_RightPanelWidth";
    private const string BalanceCardWidthKey = "Dashboard_BalanceCardWidth";
    private const string TopSectionHeightKey = "Dashboard_TopSectionHeight";

    public const double DefaultRightPanelWidth = 380;
    public const double DefaultBalanceCardWidth = 300;
    public const double DefaultTopSectionHeight = 252;

    private readonly ShellViewModel _shellViewModel;
    private string _defaultChartTooltipText = "0 đ";
    private double _currentPlotHeight = 116;

    public event EventHandler? WeeklyActivityUpdated;
    public event EventHandler? LayoutResetRequested;

    public double RightPanelWidth { get; private set; } = DefaultRightPanelWidth;
    public double BalanceCardWidth { get; private set; } = DefaultBalanceCardWidth;
    public double TopSectionHeight { get; private set; } = DefaultTopSectionHeight;

    [ObservableProperty]
    public partial string TotalBalanceText { get; set; } = "0 đ";

    [ObservableProperty]
    public partial string GrowthText { get; set; } = "+0.0%";

    [ObservableProperty]
    public partial string GrowthGlyph { get; set; } = "\uE70E";

    [ObservableProperty]
    public partial Brush GrowthBadgeBrush { get; set; } = Formatters.MintBrush;

    [ObservableProperty]
    public partial Brush GrowthTextBrush { get; set; } = Formatters.MintBrush;

    [ObservableProperty]
    public partial string SpentAmountText { get; set; } = "0 đ";

    [ObservableProperty]
    public partial string SpentTargetText { get; set; } = "/ 15,000,000 đ";

    [ObservableProperty]
    public partial string MemberLevel { get; set; } = "Mới bắt đầu";

    [ObservableProperty]
    public partial string LimitSpentText { get; set; } = "0 đ ";

    [ObservableProperty]
    public partial string LimitTargetText { get; set; } = "/ 15,000,000 đ";

    [ObservableProperty]
    public partial double LimitPercentage { get; set; } = 0;

    [ObservableProperty]
    public partial string ChartTooltipText { get; set; } = "0 đ";

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Visibility UnreadBadgeVisibility { get; set; } = Visibility.Collapsed;

    [ObservableProperty]
    public partial string NotificationCountText { get; set; } = "0 thông báo";

    public ObservableCollection<TransactionItem> RecentTransactions { get; } = new();

    public ObservableCollection<DailyActivityItem> WeeklyActivities { get; } = new();

    public ObservableCollection<NotificationItem> Notifications { get; } = new();

    public DashboardViewModel(IFinanceService financeService, ShellViewModel shellViewModel)
        : base(financeService)
    {
        _shellViewModel = shellViewModel;
        LoadLayoutSettings();
        RefreshData();
    }

    protected override void OnDataRefreshed()
    {
        RefreshData();
    }

    partial void OnSearchTextChanged(string value)
    {
        RefreshRecentTransactions();
    }

    public void RefreshData()
    {
        DateTime now = DateTime.Today;
        DateTime startOfMonth = new DateTime(now.Year, now.Month, 1);
        DateTime endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        var monthlySummary = FinanceService.GetSummary(startOfMonth, endOfMonth);

        // 1. Total Balance across all accounts
        TotalBalanceText = Formatters.Currency(monthlySummary.TotalAssets);

        // 2. Real Monthly Growth / Savings Rate
        decimal monthlyIncome = monthlySummary.TotalIncome;
        decimal monthlyExpense = monthlySummary.TotalExpense;
        if (monthlyIncome > 0)
        {
            decimal netRate = (monthlyIncome - monthlyExpense) / monthlyIncome * 100m;
            bool isPositive = netRate >= 0;
            string prefix = isPositive ? "+" : "";
            GrowthText = $"{prefix}{netRate:0.0}% tháng này";
            GrowthGlyph = isPositive ? "\uE70E" : "\uE70D";
            GrowthBadgeBrush = isPositive ? Formatters.MintBrush : Formatters.RedBrush;
            GrowthTextBrush = isPositive ? Formatters.MintBrush : Formatters.RedBrush;
        }
        else if (monthlyExpense > 0)
        {
            GrowthText = $"-{Formatters.CompactCurrency(monthlyExpense)} tháng này";
            GrowthGlyph = "\uE70D";
            GrowthBadgeBrush = Formatters.RedBrush;
            GrowthTextBrush = Formatters.RedBrush;
        }
        else
        {
            GrowthText = "0.0% tháng này";
            GrowthGlyph = "\uE70E";
            GrowthBadgeBrush = Formatters.MintBrush;
            GrowthTextBrush = Formatters.MintBrush;
        }

        // 3. Monthly Spending & Configurable Target calculation
        decimal monthlyTarget = 15_000_000m;
        string savedBudget = FinanceService.GetSetting("Monthly_Budget_Target", "15000000");
        if (decimal.TryParse(savedBudget, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsedTarget) && parsedTarget > 0)
        {
            monthlyTarget = parsedTarget;
        }

        SpentAmountText = Formatters.CompactCurrency(monthlyExpense) + " đ";
        SpentTargetText = "/ " + Formatters.CompactCurrency(monthlyTarget) + " đ";

        double ratio = monthlyTarget > 0 ? (double)(monthlyExpense / monthlyTarget) : 0;
        MemberLevel = ratio switch
        {
            < 0.25 => "Tiết kiệm xuất sắc",
            < 0.6 => "Chi tiêu hợp lý",
            < 0.85 => "Đạt mức tiêu chuẩn",
            < 1.0 => "Sắp chạm hạn mức",
            _ => "Vượt hạn mức chi tiêu"
        };

        // 4. Limit Progress
        LimitSpentText = Formatters.CompactCurrency(monthlyExpense) + " đ ";
        LimitTargetText = "/ " + Formatters.CompactCurrency(monthlyTarget) + " đ";
        LimitPercentage = Math.Min(100, Math.Max(0, ratio * 100));

        // 5. Recent transactions (filtered by SearchText if any)
        RefreshRecentTransactions();

        // 6. Weekly Activity data & spline curve
        decimal weekTotal = ComputeWeeklyActivity();

        // 7. Real Notifications
        BuildNotifications(monthlyExpense, monthlyTarget, ratio, weekTotal);
    }

    private void RefreshRecentTransactions()
    {
        RecentTransactions.Clear();
        var allCategories = FinanceService.GetAllCategories().ToDictionary(c => c.Id, c => c.Name);

        string? keyword = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();
        var latestTransactions = FinanceService.GetTransactions(keyword: keyword).Take(6).ToList();

        foreach (var t in latestTransactions)
        {
            string catName = (t.CategoryId != null && allCategories.TryGetValue(t.CategoryId, out var name))
                ? name
                : (t.Type == TransactionType.Transfer ? "Chuyển tiền" : "Khác");

            string title = string.IsNullOrWhiteSpace(t.Note) ? catName : t.Note;
            string dateStr = Formatters.RelativeDate(t.TransactionDate);

            string sign = t.Type == TransactionType.Income ? "+" : (t.Type == TransactionType.Expense ? "-" : "");
            string amountStr = $"{sign}{Formatters.Currency(t.Amount)}";

            var (amountBrush, badgeBgBrush, glyph) = t.Type switch
            {
                TransactionType.Income => (Formatters.MintBrush, Formatters.MintBgBrush, "\uE73E"),
                TransactionType.Transfer => (Formatters.PurpleBrush, Formatters.PurpleBgBrush, "\uE8AB"),
                _ => (Formatters.RedBrush, Formatters.RedBgBrush, "\uE719")
            };

            RecentTransactions.Add(new TransactionItem(
                t.Id,
                title,
                dateStr,
                amountStr,
                amountBrush,
                badgeBgBrush,
                glyph,
                t
            ));
        }
    }

    private decimal ComputeWeeklyActivity()
    {
        WeeklyActivities.Clear();
        DateTime today = DateTime.Today;
        var days = Enumerable.Range(0, 7)
            .Select(i => today.AddDays(-6 + i))
            .ToList();

        var weekTx = FinanceService.GetTransactions(fromDate: days.First(), toDate: days.Last())
            .Where(t => t.Type == TransactionType.Expense)
            .ToList();

        decimal maxSpend = 0m;
        decimal weekTotal = 0m;
        var dailySpends = new List<(DateTime Date, decimal Spend)>(7);

        foreach (var day in days)
        {
            decimal dayTotal = weekTx
                .Where(t => t.TransactionDate.Date == day.Date)
                .Sum(t => t.Amount);

            weekTotal += dayTotal;
            if (dayTotal > maxSpend) maxSpend = dayTotal;
            dailySpends.Add((day, dayTotal));
        }

        double maxBarHeight = GetMaxBarHeight(_currentPlotHeight);

        foreach (var (date, spend) in dailySpends)
        {
            bool isToday = date.Date == today;
            double normalized = maxSpend > 0 ? (double)(spend / maxSpend) : 0.0;
            double height = CalculateBarHeight(normalized, maxBarHeight);
            double opacity = spend <= 0 ? 0.16 : (isToday ? 0.58 : 0.28);

            WeeklyActivities.Add(new DailyActivityItem(
                GetVietnameseShortDayName(date),
                date.ToString("dd/MM"),
                spend,
                Formatters.Currency(spend),
                normalized,
                height,
                opacity,
                isToday
            ));
        }

        decimal todaySpend = dailySpends.Last().Spend;
        _defaultChartTooltipText = todaySpend > 0
            ? $"Hôm nay: {Formatters.Currency(todaySpend)}"
            : $"7 ngày: {Formatters.Currency(weekTotal)}";
        ChartTooltipText = _defaultChartTooltipText;

        WeeklyActivityUpdated?.Invoke(this, EventArgs.Empty);
        return weekTotal;
    }

    public void UpdateChartPlotHeight(double plotHeight)
    {
        if (plotHeight <= 0)
            return;

        _currentPlotHeight = plotHeight;
        double maxBarHeight = GetMaxBarHeight(plotHeight);
        foreach (var item in WeeklyActivities)
        {
            item.BarHeight = CalculateBarHeight(item.NormalizedValue, maxBarHeight);
        }
    }

    public void SetHoveredDay(DailyActivityItem? item)
    {
        ChartTooltipText = item is null
            ? _defaultChartTooltipText
            : $"{item.DayName} ({item.FullDateLabel}): {item.AmountText}";
    }

    private static double GetMaxBarHeight(double plotHeight) =>
        Math.Max(32.0, plotHeight - 28.0);

    private static double CalculateBarHeight(double normalizedValue, double maxBarHeight) =>
        normalizedValue <= 0 ? 4.0 : Math.Max(8.0, normalizedValue * maxBarHeight);

    private static string GetVietnameseShortDayName(DateTime date) => date.DayOfWeek switch
    {
        DayOfWeek.Monday => "T2",
        DayOfWeek.Tuesday => "T3",
        DayOfWeek.Wednesday => "T4",
        DayOfWeek.Thursday => "T5",
        DayOfWeek.Friday => "T6",
        DayOfWeek.Saturday => "T7",
        DayOfWeek.Sunday => "CN",
        _ => date.ToString("ddd", CultureInfo.InvariantCulture)
    };

    private void LoadLayoutSettings()
    {
        RightPanelWidth = LoadClampedSetting(RightPanelWidthKey, DefaultRightPanelWidth, 280, 560);
        BalanceCardWidth = LoadClampedSetting(BalanceCardWidthKey, DefaultBalanceCardWidth, 230, 440);
        TopSectionHeight = LoadClampedSetting(TopSectionHeightKey, DefaultTopSectionHeight, 210, 380);
    }

    private double LoadClampedSetting(string key, double fallback, double min, double max)
    {
        string raw = FinanceService.GetSetting(key, string.Empty);
        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            return Math.Clamp(value, min, max);
        return fallback;
    }

    public void SaveRightPanelWidth(double width)
    {
        RightPanelWidth = Math.Clamp(width, 280, 560);
        FinanceService.SetSetting(RightPanelWidthKey, RightPanelWidth.ToString("0.#", CultureInfo.InvariantCulture));
    }

    public void SaveBalanceCardWidth(double width)
    {
        BalanceCardWidth = Math.Clamp(width, 230, 440);
        FinanceService.SetSetting(BalanceCardWidthKey, BalanceCardWidth.ToString("0.#", CultureInfo.InvariantCulture));
    }

    public void SaveTopSectionHeight(double height)
    {
        TopSectionHeight = Math.Clamp(height, 210, 380);
        FinanceService.SetSetting(TopSectionHeightKey, TopSectionHeight.ToString("0.#", CultureInfo.InvariantCulture));
    }

    [RelayCommand]
    public void ResetLayout()
    {
        SaveRightPanelWidth(DefaultRightPanelWidth);
        SaveBalanceCardWidth(DefaultBalanceCardWidth);
        SaveTopSectionHeight(DefaultTopSectionHeight);
        LayoutResetRequested?.Invoke(this, EventArgs.Empty);
    }

    private void BuildNotifications(decimal monthlyExpense, decimal monthlyTarget, double ratio, decimal weekTotal)
    {
        Notifications.Clear();

        // 1. Budget alert
        if (ratio >= 0.85)
        {
            Notifications.Add(new NotificationItem(
                "Cảnh báo hạn mức chi tiêu",
                $"Bạn đã chi {Formatters.Currency(monthlyExpense)} ({ratio * 100:0.#}% hạn mức tháng).",
                "Tháng này",
                "\uE7BA",
                Formatters.RedBrush,
                Formatters.RedBgBrush
            ));
        }
        else
        {
            Notifications.Add(new NotificationItem(
                "Ngân sách trong tầm kiểm soát",
                $"Đã chi {Formatters.Currency(monthlyExpense)} / {Formatters.Currency(monthlyTarget)} ({ratio * 100:0.#}%).",
                "Tháng này",
                "\uE73E",
                Formatters.MintBrush,
                Formatters.MintBgBrush
            ));
        }

        // 2. Low balance check across accounts
        var accounts = FinanceService.GetAllAccounts();
        foreach (var acc in accounts.Where(a => a.CurrentBalance <= 200_000m))
        {
            Notifications.Add(new NotificationItem(
                $"Số dư thấp: {acc.Name}",
                $"Số dư hiện tại chỉ còn {Formatters.Currency(acc.CurrentBalance, acc.Currency)}.",
                "Ví & Tài khoản",
                "\uE7BA",
                Formatters.YellowBrush,
                Formatters.YellowBgBrush
            ));
        }

        // 3. Weekly spending summary
        Notifications.Add(new NotificationItem(
            "Tổng kết 7 ngày qua",
            $"Tổng chi tiêu 7 ngày gần nhất: {Formatters.Currency(weekTotal)}.",
            "7 ngày qua",
            "\uE9D2",
            Formatters.PurpleBrush,
            Formatters.PurpleBgBrush
        ));

        // 4. Latest transaction notification
        var latestTx = FinanceService.GetTransactions().FirstOrDefault();
        if (latestTx != null)
        {
            string sign = latestTx.Type == TransactionType.Income
                ? "+"
                : (latestTx.Type == TransactionType.Expense ? "-" : "");
            var (txAccent, txBg, txGlyph) = latestTx.Type switch
            {
                TransactionType.Income => (Formatters.MintBrush, Formatters.MintBgBrush, "\uE73E"),
                TransactionType.Transfer => (Formatters.PurpleBrush, Formatters.PurpleBgBrush, "\uE8AB"),
                _ => (Formatters.RedBrush, Formatters.RedBgBrush, "\uE719")
            };

            Notifications.Add(new NotificationItem(
                string.IsNullOrWhiteSpace(latestTx.Note) ? "Giao dịch mới nhất" : latestTx.Note,
                $"{sign}{Formatters.Currency(latestTx.Amount)}",
                Formatters.RelativeDate(latestTx.TransactionDate),
                txGlyph,
                txAccent,
                txBg
            ));
        }

        NotificationCountText = $"{Notifications.Count} thông báo";
        UnreadBadgeVisibility = Notifications.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    [RelayCommand]
    public void MarkNotificationsRead()
    {
        UnreadBadgeVisibility = Visibility.Collapsed;
    }

    [RelayCommand]
    public void GoToTransactions()
    {
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            _shellViewModel.NavigateToTransactionsWithSearch(SearchText);
        }
        else
        {
            _shellViewModel.NavigateTo("Transactions");
        }
    }

    [RelayCommand]
    public void GoToAccounts()
    {
        _shellViewModel.NavigateTo("Accounts");
    }
}
