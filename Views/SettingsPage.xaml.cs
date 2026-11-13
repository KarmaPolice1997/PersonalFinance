using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using AtomFinance.WinUI.ViewModels;

namespace AtomFinance.WinUI.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Required;

        ViewModel = new SettingsViewModel(App.FinanceService, App.DialogService, App.ThemeService);
    }

    public bool IsThemeOptionSelected(int selectedIndex, int targetIndex)
    {
        return selectedIndex == targetIndex;
    }

    private void OnThemeOptionChecked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string tagStr && int.TryParse(tagStr, out int index))
        {
            ViewModel.SelectedThemeIndex = index;
        }
    }
}
