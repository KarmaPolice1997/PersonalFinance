using System.Globalization;
using System.Runtime.InteropServices;
using PersonalFinance.Services;

namespace PersonalFinance.UI;

public enum AppTheme
{
    Device = 0, // Theo thiết bị (Hệ thống)
    Light = 1,  // Sáng (Trắng)
    Dark = 2    // Tối (Đen)
}

public static class Theme
{
    // Font: 100% Segoe UI chuẩn
    public static readonly Font FontApp = new("Segoe UI", 9.5F, FontStyle.Regular);
    public static readonly Font FontAppBold = new("Segoe UI", 9.5F, FontStyle.Bold);
    public static readonly Font FontHeader = new("Segoe UI", 10.5F, FontStyle.Bold);
    public static readonly Font FontTitle = new("Segoe UI", 13F, FontStyle.Bold);
    public static readonly Font FontStat = new("Segoe UI", 15F, FontStyle.Bold);

    public static AppTheme CurrentMode { get; private set; } = AppTheme.Device;
    public static event EventHandler? ThemeChanged;

    public static bool IsDark => CurrentMode == AppTheme.Dark || (CurrentMode == AppTheme.Device && IsSystemDark());

    public static bool IsSystemDark()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var val = key?.GetValue("AppsUseLightTheme");
            if (val is int intVal)
            {
                return intVal == 0;
            }
        }
        catch { }
        return false;
    }

    public static void Initialize(IFinanceService service)
    {
        string saved = service.GetSetting("ThemeMode", "Device");
        if (Enum.TryParse<AppTheme>(saved, out var mode))
        {
            CurrentMode = mode;
        }
        else
        {
            CurrentMode = AppTheme.Device;
        }

        try
        {
            Microsoft.Win32.SystemEvents.UserPreferenceChanged += (s, e) =>
            {
                if (CurrentMode == AppTheme.Device)
                {
                    ThemeChanged?.Invoke(null, EventArgs.Empty);
                }
            };
        }
        catch { }
    }

    public static void SetTheme(AppTheme mode, IFinanceService? service = null)
    {
        CurrentMode = mode;
        service?.SetSetting("ThemeMode", mode.ToString());
        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    // Windows Title Bar Dark Mode via DWM
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    public static void ApplyTitleBarTheme(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero) return;
        int isDark = IsDark ? 1 : 0;
        if (DwmSetWindowAttribute(hWnd, 20, ref isDark, sizeof(int)) != 0)
        {
            DwmSetWindowAttribute(hWnd, 19, ref isDark, sizeof(int));
        }
    }

    // Màu sắc Solid Flat - Tự động thích ứng Dark / Light
    public static Color BgApp => IsDark ? Color.FromArgb(24, 24, 28) : Color.FromArgb(245, 245, 247);
    public static Color BgPanel => IsDark ? Color.FromArgb(36, 36, 42) : Color.White;
    public static Color BgInput => IsDark ? Color.FromArgb(46, 46, 52) : Color.White;
    public static Color BgButton => IsDark ? Color.FromArgb(50, 50, 56) : Color.FromArgb(242, 242, 245);
    public static Color BorderColor => IsDark ? Color.FromArgb(64, 64, 72) : Color.FromArgb(215, 215, 220);

    public static Color TextPrimary => IsDark ? Color.FromArgb(245, 245, 247) : Color.FromArgb(20, 20, 20);
    public static Color TextSecondary => IsDark ? Color.FromArgb(160, 160, 168) : Color.FromArgb(100, 100, 108);

    public static Color ColorIncome => IsDark ? Color.FromArgb(74, 222, 128) : Color.FromArgb(21, 128, 61);
    public static Color ColorExpense => IsDark ? Color.FromArgb(248, 113, 113) : Color.FromArgb(185, 28, 28);
    public static Color ColorTransfer => IsDark ? Color.FromArgb(251, 191, 36) : Color.FromArgb(180, 83, 9);
    public static Color ColorSavings => IsDark ? Color.FromArgb(96, 165, 250) : Color.FromArgb(29, 78, 216);

    public static Color HeaderBg => IsDark ? Color.FromArgb(30, 30, 35) : Color.FromArgb(240, 240, 243);
    public static Color HeaderFg => IsDark ? Color.FromArgb(235, 235, 240) : Color.FromArgb(40, 40, 40);
    public static Color GridLine => IsDark ? Color.FromArgb(52, 52, 58) : Color.FromArgb(225, 225, 228);
    public static Color SelectionBg => IsDark ? Color.FromArgb(30, 58, 138) : Color.FromArgb(219, 234, 254);
    public static Color SelectionFg => IsDark ? Color.White : Color.FromArgb(30, 58, 138);
    public static Color AlternateRow => IsDark ? Color.FromArgb(32, 32, 38) : Color.FromArgb(250, 250, 252);

    public static string FormatMoney(decimal amount, string currency = "VND")
    {
        var nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        nfi.NumberGroupSeparator = ".";
        return $"{amount.ToString("#,##0", nfi)} {currency}";
    }

    public static string FormatDate(DateTime date)
    {
        return date.ToString("dd/MM/yyyy");
    }

    public static void FormatGrid(DataGridView grid)
    {
        grid.Font = FontApp;
        grid.BackgroundColor = BgPanel;
        grid.BorderStyle = BorderStyle.Fixed3D;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.Single;
        grid.GridColor = GridLine;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.ReadOnly = true;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.RowTemplate.Height = 28;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersHeight = 32;

        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        grid.ColumnHeadersDefaultCellStyle.BackColor = HeaderBg;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = HeaderFg;
        grid.ColumnHeadersDefaultCellStyle.Font = FontAppBold;

        grid.DefaultCellStyle.BackColor = BgPanel;
        grid.DefaultCellStyle.ForeColor = TextPrimary;
        grid.DefaultCellStyle.SelectionBackColor = SelectionBg;
        grid.DefaultCellStyle.SelectionForeColor = SelectionFg;

        grid.AlternatingRowsDefaultCellStyle.BackColor = AlternateRow;
        grid.AlternatingRowsDefaultCellStyle.ForeColor = TextPrimary;
    }

    public static void ApplyTheme(Form form)
    {
        form.BackColor = BgApp;
        form.ForeColor = TextPrimary;
        ApplyTitleBarTheme(form.Handle);
        ApplyThemeRecursive(form);
    }

    private static void ApplyThemeRecursive(Control parent)
    {
        foreach (Control c in parent.Controls)
        {
            if (c is MenuStrip ms)
            {
                ms.BackColor = BgPanel;
                ms.ForeColor = TextPrimary;
                ms.Renderer = new ToolStripProfessionalRenderer(new AppColorTable());
                foreach (ToolStripItem item in ms.Items)
                {
                    ApplyToolStripItemTheme(item);
                }
            }
            else if (c is StatusStrip ss)
            {
                ss.BackColor = BgPanel;
                ss.ForeColor = TextSecondary;
                foreach (ToolStripItem item in ss.Items)
                {
                    item.ForeColor = TextSecondary;
                }
            }
            else if (c is TabControl tc)
            {
                tc.BackColor = BgPanel;
                tc.ForeColor = TextPrimary;
                foreach (TabPage tp in tc.TabPages)
                {
                    tp.BackColor = BgPanel;
                    tp.ForeColor = TextPrimary;
                    ApplyThemeRecursive(tp);
                }
            }
            else if (c is GroupBox gb)
            {
                gb.BackColor = BgPanel;
                gb.ForeColor = TextPrimary;
                ApplyThemeRecursive(gb);
            }
            else if (c is Panel p)
            {
                if (p.BackColor != Color.Transparent)
                {
                    p.BackColor = BgPanel;
                }
                p.ForeColor = TextPrimary;
                ApplyThemeRecursive(p);
            }
            else if (c is Button btn)
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = BorderColor;
                btn.FlatAppearance.BorderSize = 1;
                btn.BackColor = BgButton;
                btn.ForeColor = TextPrimary;
            }
            else if (c is TextBox txt)
            {
                txt.BackColor = BgInput;
                txt.ForeColor = TextPrimary;
                txt.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (c is NumericUpDown num)
            {
                num.BackColor = BgInput;
                num.ForeColor = TextPrimary;
                num.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (c is ComboBox cbo)
            {
                cbo.BackColor = BgInput;
                cbo.ForeColor = TextPrimary;
                cbo.FlatStyle = FlatStyle.Flat;
            }
            else if (c is DateTimePicker dtp)
            {
                dtp.CalendarMonthBackground = BgPanel;
                dtp.CalendarForeColor = TextPrimary;
                dtp.CalendarTitleBackColor = BgPanel;
                dtp.CalendarTitleForeColor = TextPrimary;
            }
            else if (c is DataGridView grid)
            {
                FormatGrid(grid);
            }
            else if (c is Label lbl)
            {
                if (lbl.Tag is not string tag || tag != "semantic")
                {
                    lbl.ForeColor = lbl.ForeColor == SystemColors.GrayText ? TextSecondary : TextPrimary;
                }
            }
            else if (c.HasChildren)
            {
                ApplyThemeRecursive(c);
            }
        }
    }

    private static void ApplyToolStripItemTheme(ToolStripItem item)
    {
        item.ForeColor = TextPrimary;
        if (item is ToolStripDropDownItem dropDown)
        {
            dropDown.DropDown.BackColor = BgPanel;
            dropDown.DropDown.ForeColor = TextPrimary;
            foreach (ToolStripItem subItem in dropDown.DropDownItems)
            {
                ApplyToolStripItemTheme(subItem);
            }
        }
    }
}

public class AppColorTable : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground => Theme.BgPanel;
    public override Color MenuBorder => Theme.BorderColor;
    public override Color MenuItemBorder => Theme.BorderColor;
    public override Color MenuItemSelected => Theme.BgButton;
    public override Color MenuStripGradientBegin => Theme.BgPanel;
    public override Color MenuStripGradientEnd => Theme.BgPanel;
    public override Color MenuItemSelectedGradientBegin => Theme.BgButton;
    public override Color MenuItemSelectedGradientEnd => Theme.BgButton;
    public override Color MenuItemPressedGradientBegin => Theme.BgButton;
    public override Color MenuItemPressedGradientEnd => Theme.BgButton;
    public override Color ImageMarginGradientBegin => Theme.BgPanel;
    public override Color ImageMarginGradientMiddle => Theme.BgPanel;
    public override Color ImageMarginGradientEnd => Theme.BgPanel;
    public override Color SeparatorDark => Theme.BorderColor;
    public override Color SeparatorLight => Theme.BorderColor;
}
