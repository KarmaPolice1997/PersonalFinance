using Microsoft.UI.Xaml;
using PersonalFinance.Services;

namespace AtomFinance.WinUI.Services;

public class ThemeService
{
    private const string SettingKey = "App_Theme_Mode";
    private readonly IFinanceService _financeService;
    private Window? _window;

    public ElementTheme CurrentTheme { get; private set; } = ElementTheme.Default;

    public event EventHandler<ElementTheme>? ThemeChanged;

    public ThemeService(IFinanceService financeService)
    {
        _financeService = financeService;
    }

    public void Initialize(Window window)
    {
        _window = window;

        string saved = _financeService.GetSetting(SettingKey, "Default");
        CurrentTheme = saved switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        ApplyTheme(CurrentTheme, save: false);
    }

    public void SetTheme(ElementTheme theme)
    {
        ApplyTheme(theme, save: true);
    }

    public void ToggleTheme()
    {
        var next = CurrentTheme switch
        {
            ElementTheme.Light => ElementTheme.Dark,
            ElementTheme.Dark => ElementTheme.Default,
            _ => ElementTheme.Light
        };
        SetTheme(next);
    }

    private void ApplyTheme(ElementTheme theme, bool save)
    {
        CurrentTheme = theme;

        if (_window?.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme;
        }

        if (save)
        {
            _financeService.SetSetting(SettingKey, theme.ToString());
        }

        ThemeChanged?.Invoke(this, theme);
    }
}
