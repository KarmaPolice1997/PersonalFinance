using PersonalFinance.Models;

namespace AtomFinance.WinUI.Services;

public interface IDialogService
{
    Task ShowMessageAsync(string title, string message);
    Task<bool> ShowConfirmAsync(string title, string message, string confirmText = "Đồng ý", string cancelText = "Hủy");
    Task<Transaction?> ShowAddTransactionDialogAsync();
    Task<Transaction?> ShowEditTransactionDialogAsync(Transaction existing);
    Task<Account?> ShowAddAccountDialogAsync();
    Task<Account?> ShowEditAccountDialogAsync(Account existing);
    Task ShowManageCategoriesDialogAsync();
    Task<string?> PickSaveDatabaseFileAsync();
    Task<string?> PickOpenDatabaseFileAsync();
}
