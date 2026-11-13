using Microsoft.UI.Xaml;
using AtomFinance.WinUI.ViewModels;

namespace AtomFinance.WinUI;

public sealed partial class MainWindow : Window
{
    public ShellViewModel ViewModel => App.ShellViewModel;

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1360, 860));

        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ShellViewModel.ActivePage))
            {
                UpdateNavIndicators(ViewModel.ActivePage);
            }
        };

        NavDashboardBtn.Click += (s, e) => ViewModel.NavigateTo("Dashboard");
        NavTransactionsBtn.Click += (s, e) => ViewModel.NavigateTo("Transactions");
        NavAccountsBtn.Click += (s, e) => ViewModel.NavigateTo("Accounts");
        NavSettingsBtn.Click += (s, e) => ViewModel.NavigateTo("Settings");
        LogoBtn.Click += (s, e) => ViewModel.NavigateTo("Dashboard");
        ThemeToggleBtn.Click += (s, e) => ViewModel.ToggleTheme();

        // Initialize frame in ShellViewModel and navigate to Dashboard
        ViewModel.SetFrame(RootFrame);
        UpdateNavIndicators("Dashboard");
    }

    private void UpdateNavIndicators(string activePage)
    {
        IndicatorDashboard.Visibility = activePage == "Dashboard" ? Visibility.Visible : Visibility.Collapsed;
        IndicatorTransactions.Visibility = activePage == "Transactions" ? Visibility.Visible : Visibility.Collapsed;
        IndicatorAccounts.Visibility = activePage == "Accounts" ? Visibility.Visible : Visibility.Collapsed;
        IndicatorSettings.Visibility = activePage == "Settings" ? Visibility.Visible : Visibility.Collapsed;
    }
}
