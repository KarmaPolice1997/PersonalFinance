using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using AtomFinance.WinUI.Helpers;
using AtomFinance.WinUI.Models;
using PersonalFinance.Models;
using PersonalFinance.Services;

namespace AtomFinance.WinUI.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly ShellViewModel _shellViewModel;

    [ObservableProperty]
    public partial string TotalBalanceText { get; set; } = "0 đ";

    [ObservableProperty]
    public partial string GrowthText { get; set; } = "+0.0%";

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
    public partial string ChartPathData { get; set; } = "M 0,90 L 360,90";

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
            string prefix = netRate >= 0 ? "+" : "";
            GrowthText = $"{prefix}{netRate:0.0}% tháng này";
        }
        else if (monthlyExpense > 0)
        {
            GrowthText = $"-{Formatters.CompactCurrency(monthlyExpense)} tháng này";
        }
        else
        {
            GrowthText = "0.0% tháng này";
        }

        // 3. Monthly Spending & Target calculation
        decimal monthlyTarget = 15_000_000m;
        if (monthlyExpense > monthlyTarget)
            monthlyTarget = Math.Ceiling(monthlyExpense / 10_000_000m + 0.5m) * 10_000_000m;

        SpentAmountText = Formatters.CompactCurrency(monthlyExpense) + " đ";
        SpentTargetText = "/ " + Formatters.CompactCurrency(monthlyTarget) + " đ";

        double ratio = monthlyTarget > 0 ? (double)(monthlyExpense / monthlyTarget) : 0;
        MemberLevel = ratio switch
        {
            < 0.25 => "Tiết kiệm xuất sắc",
            < 0.6 => "Chi tiêu hợp lý",
            < 0.85 => "Đạt mức tiêu chuẩn",
            _ => "Cần kiểm soát chi tiêu"
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
                TransactionType.Income => (Formatters.MintBrush, Formatters.MintBgBrush, ""),
                TransactionType.Transfer => (Formatters.PurpleBrush, Formatters.PurpleBgBrush, ""),
                _ => (Formatters.RedBrush, Formatters.RedBgBrush, "")
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

        decimal maxSpend = 1m;
        decimal weekTotal = 0m;
        var dailySpends = new List<(DateTime Date, decimal Spend)>();

        foreach (var day in days)
        {
            decimal dayTotal = weekTx
                .Where(t => t.TransactionDate.Date == day.Date)
                .Sum(t => t.Amount);

            weekTotal += dayTotal;
            if (dayTotal > maxSpend) maxSpend = dayTotal;
            dailySpends.Add((day, dayTotal));
        }

        foreach (var (date, spend) in dailySpends)
        {
            double height = maxSpend > 0 ? (double)(spend / maxSpend * 72m) : 6.0;
            if (height < 6) height = 6;

            WeeklyActivities.Add(new DailyActivityItem(
                date.ToString("ddd"),
                spend,
                Formatters.Currency(spend),
                height
            ));
        }

        // Build smooth SVG Bezier path across the 7 points (Canvas width ~360, height ~100)
        var pts = new (double X, double Y)[dailySpends.Count];
        for (int i = 0; i < dailySpends.Count; i++)
        {
            double x = i * (360.0 / 6.0);
            double norm = maxSpend > 0 ? (double)(dailySpends[i].Spend / maxSpend) : 0.0;
            double y = 95.0 - (norm * 70.0); // y in [25..95]
            pts[i] = (x, y);
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendFormat(CultureInfo.InvariantCulture, "M {0:0.#},{1:0.#}", pts[0].X, pts[0].Y);
        for (int i = 0; i < pts.Length - 1; i++)
        {
            double midX = (pts[i].X + pts[i + 1].X) / 2.0;
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                " C {0:0.#},{1:0.#} {2:0.#},{3:0.#} {4:0.#},{5:0.#}",
                midX, pts[i].Y,
                midX, pts[i + 1].Y,
                pts[i + 1].X, pts[i + 1].Y);
        }
        ChartPathData = sb.ToString();

        decimal todaySpend = dailySpends.Last().Spend;
        ChartTooltipText = todaySpend > 0
            ? $"Hôm nay: {Formatters.Currency(todaySpend)}"
            : $"7 ngày: {Formatters.Currency(weekTotal)}";

        return weekTotal;
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
                "",
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
                "",
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
                "",
                Formatters.YellowBrush,
                Formatters.YellowBgBrush
            ));
        }

        // 3. Weekly spending summary
        Notifications.Add(new NotificationItem(
            "Tổng kết 7 ngày qua",
            $"Tổng chi tiêu 7 ngày gần nhất: {Formatters.Currency(weekTotal)}.",
            "7 ngày qua",
            "",
            Formatters.PurpleBrush,
            Formatters.PurpleBgBrush
        ));

        // 4. Latest transaction notification
        var latestTx = FinanceService.GetTransactions().FirstOrDefault();
        if (latestTx != null)
        {
            string sign = latestTx.Type == TransactionType.Income ? "+" : "-";
            Notifications.Add(new NotificationItem(
                string.IsNullOrWhiteSpace(latestTx.Note) ? "Giao dịch mới nhất" : latestTx.Note,
                $"{sign}{Formatters.Currency(latestTx.Amount)}",
                Formatters.RelativeDate(latestTx.TransactionDate),
                "",
                Formatters.MintBrush,
                Formatters.MintBgBrush
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
