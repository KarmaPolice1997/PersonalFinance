using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using AtomFinance.WinUI.ViewModels;

namespace AtomFinance.WinUI.Views;

public sealed partial class AccountsPage : Page
{
    public AccountsViewModel ViewModel { get; }

    public AccountsPage()
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Required;

        ViewModel = new AccountsViewModel(App.FinanceService, App.DialogService);
    }

    private async void OnDeleteClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string id)
        {
            await ViewModel.DeleteAccountAsync(id);
        }
    }
}
