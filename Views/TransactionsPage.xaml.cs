using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using AtomFinance.WinUI.ViewModels;

namespace AtomFinance.WinUI.Views;

public sealed partial class TransactionsPage : Page
{
    public TransactionsViewModel ViewModel { get; }

    public TransactionsPage()
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Required;

        ViewModel = new TransactionsViewModel(App.FinanceService, App.DialogService, App.ShellViewModel);
    }

    private async void OnEditClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string id)
        {
            await ViewModel.EditTransactionAsync(id);
        }
    }

    private async void OnDeleteClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string id)
        {
            await ViewModel.DeleteTransactionAsync(id);
        }
    }
}
