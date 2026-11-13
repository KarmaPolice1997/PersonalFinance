using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AtomFinance.WinUI.Helpers;
using AtomFinance.WinUI.Models;
using AtomFinance.WinUI.Services;
using PersonalFinance.Services;

namespace AtomFinance.WinUI.ViewModels;

public partial class AccountsViewModel : ViewModelBase
{
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    public partial string TotalAssetsText { get; set; } = "0 đ";

    public ObservableCollection<AccountItem> Accounts { get; } = new();

    public AccountsViewModel(IFinanceService financeService, IDialogService dialogService)
        : base(financeService)
    {
        _dialogService = dialogService;
        RefreshAccounts();
    }

    protected override void OnDataRefreshed()
    {
        RefreshAccounts();
    }

    [RelayCommand]
    public async Task AddAccountAsync()
    {
        await _dialogService.ShowAddAccountDialogAsync();
    }

    [RelayCommand]
    public async Task DeleteAccountAsync(string id)
    {
        bool confirm = await _dialogService.ShowConfirmAsync("Xác nhận xóa", "Bạn có chắc chắn muốn xóa tài khoản này không?");
        if (!confirm) return;

        if (!FinanceService.DeleteAccount(id, out string error))
        {
            await _dialogService.ShowMessageAsync("Không thể xóa", error);
        }
    }

    public void RefreshAccounts()
    {
        Accounts.Clear();
        var list = FinanceService.GetAllAccounts();

        decimal total = 0;
        int index = 0;
        foreach (var acc in list)
        {
            total += acc.CurrentBalance;
            string last4 = acc.Id.Length >= 4 ? acc.Id[^4..].ToUpperInvariant() : "1234";

            Accounts.Add(new AccountItem(
                acc.Id,
                acc.Name,
                Formatters.Currency(acc.CurrentBalance, acc.Currency),
                string.IsNullOrWhiteSpace(acc.Description) ? acc.Type.ToString() : acc.Description,
                acc.Type.ToString(),
                $"•••• {last4}",
                index % 3,
                acc
            ));
            index++;
        }

        TotalAssetsText = $"Tổng tài sản: {Formatters.Currency(total)}";
    }
}
