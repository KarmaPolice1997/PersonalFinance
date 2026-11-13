using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using AtomFinance.WinUI.Services;
using AtomFinance.WinUI.Views;

namespace AtomFinance.WinUI.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly ThemeService _themeService;
    private Frame? _contentFrame;

    [ObservableProperty]
    public partial string ActivePage { get; set; } = "Dashboard";

    [ObservableProperty]
    public partial string PendingSearchQuery { get; set; } = string.Empty;

    public ShellViewModel(ThemeService themeService)
    {
        _themeService = themeService;
    }

    public void SetFrame(Frame frame)
    {
        _contentFrame = frame;
        NavigateTo("Dashboard");
    }

    [RelayCommand]
    public void NavigateTo(string pageName)
    {
        if (_contentFrame == null) return;

        Type pageType = pageName switch
        {
            "Transactions" => typeof(TransactionsPage),
            "Accounts" => typeof(AccountsPage),
            "Settings" => typeof(SettingsPage),
            _ => typeof(DashboardPage)
        };

        if (_contentFrame.CurrentSourcePageType != pageType)
        {
            _contentFrame.Navigate(pageType);
        }

        ActivePage = pageName;
    }

    public void NavigateToTransactionsWithSearch(string query)
    {
        PendingSearchQuery = query?.Trim() ?? string.Empty;
        NavigateTo("Transactions");
    }

    [RelayCommand]
    public void ToggleTheme()
    {
        _themeService.ToggleTheme();
    }
}
