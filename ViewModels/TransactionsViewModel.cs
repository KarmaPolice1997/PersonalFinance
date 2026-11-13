using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AtomFinance.WinUI.Helpers;
using AtomFinance.WinUI.Models;
using AtomFinance.WinUI.Services;
using PersonalFinance.Models;
using PersonalFinance.Services;

namespace AtomFinance.WinUI.ViewModels;

public partial class TransactionsViewModel : ViewModelBase
{
    private readonly IDialogService _dialogService;
    private readonly ShellViewModel _shellViewModel;
    private bool _isRefreshingFilters;

    [ObservableProperty]
    public partial TransactionType? CurrentTypeFilter { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FilterSummaryText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int SelectedAccountFilterIndex { get; set; } = 0;

    [ObservableProperty]
    public partial int SelectedCategoryFilterIndex { get; set; } = 0;

    [ObservableProperty]
    public partial double AllFilterOpacity { get; set; } = 1.0;

    [ObservableProperty]
    public partial double ExpenseFilterOpacity { get; set; } = 0.55;

    [ObservableProperty]
    public partial double IncomeFilterOpacity { get; set; } = 0.55;

    [ObservableProperty]
    public partial double TransferFilterOpacity { get; set; } = 0.55;

    public ObservableCollection<TransactionItem> Transactions { get; } = new();
    public ObservableCollection<Account> FilterAccounts { get; } = new();
    public ObservableCollection<Category> FilterCategories { get; } = new();

    public TransactionsViewModel(IFinanceService financeService, IDialogService dialogService, ShellViewModel shellViewModel)
        : base(financeService)
    {
        _dialogService = dialogService;
        _shellViewModel = shellViewModel;

        _shellViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ShellViewModel.PendingSearchQuery) &&
                !string.IsNullOrEmpty(_shellViewModel.PendingSearchQuery))
            {
                SearchText = _shellViewModel.PendingSearchQuery;
                _shellViewModel.PendingSearchQuery = string.Empty;
            }
        };

        LoadFilterOptions();

        if (!string.IsNullOrEmpty(_shellViewModel.PendingSearchQuery))
        {
            SearchText = _shellViewModel.PendingSearchQuery;
            _shellViewModel.PendingSearchQuery = string.Empty;
        }
        else
        {
            RefreshTransactions();
        }
    }

    protected override void OnDataRefreshed()
    {
        LoadFilterOptions();
        RefreshTransactions();
    }

    partial void OnSearchTextChanged(string value)
    {
        RefreshTransactions();
    }

    partial void OnSelectedAccountFilterIndexChanged(int value)
    {
        if (!_isRefreshingFilters)
            RefreshTransactions();
    }

    partial void OnSelectedCategoryFilterIndexChanged(int value)
    {
        if (!_isRefreshingFilters)
            RefreshTransactions();
    }

    partial void OnCurrentTypeFilterChanged(TransactionType? value)
    {
        AllFilterOpacity = value == null ? 1.0 : 0.55;
        ExpenseFilterOpacity = value == TransactionType.Expense ? 1.0 : 0.55;
        IncomeFilterOpacity = value == TransactionType.Income ? 1.0 : 0.55;
        TransferFilterOpacity = value == TransactionType.Transfer ? 1.0 : 0.55;

        RefreshTransactions();
    }

    private void LoadFilterOptions()
    {
        _isRefreshingFilters = true;
        try
        {
            string? selectedAccId = SelectedAccountFilterIndex > 0 && SelectedAccountFilterIndex < FilterAccounts.Count
                ? FilterAccounts[SelectedAccountFilterIndex].Id
                : null;
            string? selectedCatId = SelectedCategoryFilterIndex > 0 && SelectedCategoryFilterIndex < FilterCategories.Count
                ? FilterCategories[SelectedCategoryFilterIndex].Id
                : null;

            FilterAccounts.Clear();
            FilterAccounts.Add(new Account { Id = string.Empty, Name = "Tất cả ví" });
            foreach (var acc in FinanceService.GetAllAccounts())
            {
                FilterAccounts.Add(acc);
            }

            FilterCategories.Clear();
            FilterCategories.Add(new Category { Id = string.Empty, Name = "Tất cả danh mục" });
            foreach (var cat in FinanceService.GetAllCategories())
            {
                FilterCategories.Add(cat);
            }

            int accIdx = selectedAccId != null
                ? FilterAccounts.ToList().FindIndex(a => a.Id == selectedAccId)
                : 0;
            SelectedAccountFilterIndex = accIdx >= 0 ? accIdx : 0;

            int catIdx = selectedCatId != null
                ? FilterCategories.ToList().FindIndex(c => c.Id == selectedCatId)
                : 0;
            SelectedCategoryFilterIndex = catIdx >= 0 ? catIdx : 0;
        }
        finally
        {
            _isRefreshingFilters = false;
        }
    }

    [RelayCommand]
    public void SetFilter(string filterName)
    {
        CurrentTypeFilter = filterName switch
        {
            "Expense" => TransactionType.Expense,
            "Income" => TransactionType.Income,
            "Transfer" => TransactionType.Transfer,
            _ => null
        };
    }

    [RelayCommand]
    public async Task AddTransactionAsync()
    {
        await _dialogService.ShowAddTransactionDialogAsync();
    }

    [RelayCommand]
    public async Task EditTransactionAsync(string id)
    {
        var item = Transactions.FirstOrDefault(t => t.Id == id);
        if (item == null) return;

        await _dialogService.ShowEditTransactionDialogAsync(item.RawTransaction);
    }

    [RelayCommand]
    public async Task ManageCategoriesAsync()
    {
        await _dialogService.ShowManageCategoriesDialogAsync();
    }

    [RelayCommand]
    public async Task DeleteTransactionAsync(string id)
    {
        bool confirm = await _dialogService.ShowConfirmAsync("Xác nhận xóa", "Bạn có chắc chắn muốn xóa giao dịch này không?");
        if (confirm)
        {
            FinanceService.DeleteTransaction(id);
        }
    }

    public void RefreshTransactions()
    {
        Transactions.Clear();

        var categories = FinanceService.GetAllCategories().ToDictionary(c => c.Id, c => c.Name);
        var accounts = FinanceService.GetAllAccounts().ToDictionary(a => a.Id, a => a.Name);

        string? accountFilterId = SelectedAccountFilterIndex > 0 && SelectedAccountFilterIndex < FilterAccounts.Count
            ? FilterAccounts[SelectedAccountFilterIndex].Id
            : null;
        string? categoryFilterId = SelectedCategoryFilterIndex > 0 && SelectedCategoryFilterIndex < FilterCategories.Count
            ? FilterCategories[SelectedCategoryFilterIndex].Id
            : null;

        var list = FinanceService.GetTransactions(
            accountId: string.IsNullOrEmpty(accountFilterId) ? null : accountFilterId,
            categoryId: string.IsNullOrEmpty(categoryFilterId) ? null : categoryFilterId,
            type: CurrentTypeFilter,
            keyword: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim()
        );

        decimal totalInc = 0;
        decimal totalExp = 0;

        foreach (var t in list)
        {
            if (t.Type == TransactionType.Income) totalInc += t.Amount;
            if (t.Type == TransactionType.Expense) totalExp += t.Amount;

            string catName = (t.CategoryId != null && categories.TryGetValue(t.CategoryId, out var name))
                ? name
                : (t.Type == TransactionType.Transfer ? "Chuyển tiền" : "Khác");

            string accName = (t.SourceAccountId != null && accounts.TryGetValue(t.SourceAccountId, out var aName))
                ? aName
                : "Tài khoản";

            string destAccName = (t.DestinationAccountId != null && accounts.TryGetValue(t.DestinationAccountId, out var daName))
                ? daName
                : string.Empty;

            string title = string.IsNullOrWhiteSpace(t.Note) ? catName : t.Note;
            string subtitle = t.Type == TransactionType.Transfer
                ? $"{accName} ➔ {destAccName} • {Formatters.RelativeDate(t.TransactionDate)}"
                : $"{catName} • {accName} • {Formatters.RelativeDate(t.TransactionDate)}";

            string sign = t.Type == TransactionType.Income ? "+" : (t.Type == TransactionType.Expense ? "-" : "");
            string amountStr = $"{sign}{Formatters.Currency(t.Amount)}";

            var (amountBrush, badgeBgBrush, glyph) = t.Type switch
            {
                TransactionType.Income => (Formatters.MintBrush, Formatters.MintBgBrush, "\uE73E"),
                TransactionType.Transfer => (Formatters.PurpleBrush, Formatters.PurpleBgBrush, "\uE8AB"),
                _ => (Formatters.RedBrush, Formatters.RedBgBrush, "\uE719")
            };

            Transactions.Add(new TransactionItem(
                t.Id,
                title,
                subtitle,
                amountStr,
                amountBrush,
                badgeBgBrush,
                glyph,
                t
            ));
        }

        FilterSummaryText = $"{list.Count} giao dịch  •  Thu: +{Formatters.Currency(totalInc)}  •  Chi: -{Formatters.Currency(totalExp)}";
    }
}
