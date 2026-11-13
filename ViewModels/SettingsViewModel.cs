using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using AtomFinance.WinUI.Services;
using PersonalFinance.Services;

namespace AtomFinance.WinUI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IFinanceService _financeService;
    private readonly IDialogService _dialogService;
    private readonly ThemeService _themeService;

    [ObservableProperty]
    public partial string DatabasePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int SelectedThemeIndex { get; set; } = 0;

    public SettingsViewModel(IFinanceService financeService, IDialogService dialogService, ThemeService themeService)
    {
        _financeService = financeService;
        _dialogService = dialogService;
        _themeService = themeService;

        DatabasePath = _financeService.DatabasePath;

        SelectedThemeIndex = _themeService.CurrentTheme switch
        {
            ElementTheme.Light => 1,
            ElementTheme.Dark => 2,
            _ => 0
        };

        _themeService.ThemeChanged += (s, theme) =>
        {
            SelectedThemeIndex = theme switch
            {
                ElementTheme.Light => 1,
                ElementTheme.Dark => 2,
                _ => 0
            };
        };
    }

    partial void OnSelectedThemeIndexChanged(int value)
    {
        var theme = value switch
        {
            1 => ElementTheme.Light,
            2 => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        if (_themeService.CurrentTheme != theme)
        {
            _themeService.SetTheme(theme);
        }
    }

    [RelayCommand]
    public async Task BackupAsync()
    {
        var targetPath = await _dialogService.PickSaveDatabaseFileAsync();
        if (string.IsNullOrWhiteSpace(targetPath)) return;

        try
        {
            _financeService.BackupDatabase(targetPath);
            await _dialogService.ShowMessageAsync("Sao lưu thành công", $"Dữ liệu đã được sao lưu an toàn tại:\n{targetPath}");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("Lỗi sao lưu", ex.Message);
        }
    }

    [RelayCommand]
    public async Task RestoreAsync()
    {
        var sourcePath = await _dialogService.PickOpenDatabaseFileAsync();
        if (string.IsNullOrWhiteSpace(sourcePath)) return;

        bool confirm = await _dialogService.ShowConfirmAsync(
            "Xác nhận phục hồi",
            $"Bạn có chắc chắn muốn thay thế toàn bộ dữ liệu hiện tại bằng file sao lưu này?\n\n{sourcePath}",
            confirmText: "Phục hồi ngay",
            cancelText: "Hủy"
        );

        if (!confirm) return;

        try
        {
            _financeService.RestoreDatabase(sourcePath);
            await _dialogService.ShowMessageAsync("Phục hồi thành công", "Toàn bộ dữ liệu đã được phục hồi thành công.");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("Lỗi phục hồi", ex.Message);
        }
    }
}
