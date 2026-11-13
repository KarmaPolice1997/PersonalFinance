using Microsoft.UI.Xaml;
using PersonalFinance.Data;
using PersonalFinance.Services;
using AtomFinance.WinUI.Services;
using AtomFinance.WinUI.ViewModels;

namespace AtomFinance.WinUI;

public partial class App : Application
{
    private Window? _window;
    private static SqliteDatabase? _sqliteDatabase;

    public static Window? MainWindowInstance => ((App)Current)._window;
    public static IFinanceService FinanceService { get; private set; } = null!;
    public static ThemeService ThemeService { get; private set; } = null!;
    public static IDialogService DialogService { get; private set; } = null!;
    public static ShellViewModel ShellViewModel { get; private set; } = null!;

    public App()
    {
        InitializeComponent();

        _sqliteDatabase = new SqliteDatabase();
        FinanceService = new FinanceService(_sqliteDatabase);
        ThemeService = new ThemeService(FinanceService);
        DialogService = new DialogService(FinanceService);
        ShellViewModel = new ShellViewModel(ThemeService);
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Closed += (s, e) => _sqliteDatabase?.Dispose();
        ThemeService.Initialize(_window);
        _window.Activate();
    }
}
