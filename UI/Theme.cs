using System.Drawing.Drawing2D;
using System.Globalization;

namespace PersonalFinance.UI;

public static class Theme
{
    // Fonts: Strictly Segoe UI
    public static readonly Font FontAppTitle = new("Segoe UI", 13F, FontStyle.Bold);
    public static readonly Font FontPageTitle = new("Segoe UI", 16F, FontStyle.Bold);
    public static readonly Font FontSectionTitle = new("Segoe UI", 11.5F, FontStyle.Bold);
    public static readonly Font FontCardValue = new("Segoe UI", 17F, FontStyle.Bold);
    public static readonly Font FontBody = new("Segoe UI", 9.5F, FontStyle.Regular);
    public static readonly Font FontBodyBold = new("Segoe UI", 9.5F, FontStyle.Bold);
    public static readonly Font FontSmall = new("Segoe UI", 8.5F, FontStyle.Regular);
    public static readonly Font FontButton = new("Segoe UI", 9.5F, FontStyle.Bold);

    // Solid Flat Colors (No gradients)
    public static readonly Color BgApp = Color.FromArgb(241, 245, 249);        // Slate 100
    public static readonly Color BgCard = Color.FromArgb(255, 255, 255);       // White
    public static readonly Color BorderCard = Color.FromArgb(226, 232, 240);   // Slate 200

    public static readonly Color SidebarBg = Color.FromArgb(15, 23, 42);       // Slate 900
    public static readonly Color SidebarHover = Color.FromArgb(30, 41, 59);    // Slate 800
    public static readonly Color SidebarActive = Color.FromArgb(37, 99, 235);  // Blue 600
    public static readonly Color SidebarText = Color.FromArgb(203, 213, 225);  // Slate 300
    public static readonly Color SidebarTextActive = Color.White;

    public static readonly Color TextPrimary = Color.FromArgb(15, 23, 42);     // Slate 900
    public static readonly Color TextSecondary = Color.FromArgb(100, 116, 139);// Slate 500
    public static readonly Color TextMuted = Color.FromArgb(148, 163, 184);    // Slate 400

    public static readonly Color Primary = Color.FromArgb(37, 99, 235);        // Blue 600
    public static readonly Color PrimaryHover = Color.FromArgb(29, 78, 216);   // Blue 700

    public static readonly Color IncomeGreen = Color.FromArgb(22, 163, 74);    // Green 600
    public static readonly Color ExpenseRed = Color.FromArgb(220, 38, 38);     // Red 600
    public static readonly Color TransferAmber = Color.FromArgb(217, 119, 6);  // Amber 600

    public static readonly Color Danger = Color.FromArgb(220, 38, 38);
    public static readonly Color Secondary = Color.FromArgb(241, 245, 249);
    public static readonly Color SecondaryBorder = Color.FromArgb(203, 213, 225);
    public static readonly Color SecondaryText = Color.FromArgb(51, 65, 85);

    // Grid row colors
    public static readonly Color GridHeaderBg = Color.FromArgb(248, 250, 252);
    public static readonly Color GridHeaderFg = Color.FromArgb(71, 85, 105);
    public static readonly Color GridRowAlternate = Color.FromArgb(248, 250, 252);
    public static readonly Color GridSelectionBg = Color.FromArgb(219, 234, 254);
    public static readonly Color GridSelectionFg = Color.FromArgb(30, 58, 138);

    public static void ApplyPrimaryButton(Button btn)
    {
        btn.Font = FontButton;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.BackColor = Primary;
        btn.ForeColor = Color.White;
        btn.Cursor = Cursors.Hand;
    }

    public static void ApplySecondaryButton(Button btn)
    {
        btn.Font = FontButton;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderColor = SecondaryBorder;
        btn.FlatAppearance.BorderSize = 1;
        btn.BackColor = Color.White;
        btn.ForeColor = SecondaryText;
        btn.Cursor = Cursors.Hand;
    }

    public static void ApplyDangerButton(Button btn)
    {
        btn.Font = FontButton;
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.BackColor = Danger;
        btn.ForeColor = Color.White;
        btn.Cursor = Cursors.Hand;
    }

    public static void FormatGrid(DataGridView grid)
    {
        grid.Font = FontBody;
        grid.BackgroundColor = BgCard;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = BorderCard;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.ReadOnly = true;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.RowTemplate.Height = 36;
        grid.EnableHeadersVisualStyles = false;

        // Header style
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        grid.ColumnHeadersDefaultCellStyle.BackColor = GridHeaderBg;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = GridHeaderFg;
        grid.ColumnHeadersDefaultCellStyle.Font = FontBodyBold;
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        grid.ColumnHeadersHeight = 40;

        // Rows style
        grid.DefaultCellStyle.BackColor = BgCard;
        grid.DefaultCellStyle.ForeColor = TextPrimary;
        grid.DefaultCellStyle.SelectionBackColor = GridSelectionBg;
        grid.DefaultCellStyle.SelectionForeColor = GridSelectionFg;
        grid.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);

        grid.AlternatingRowsDefaultCellStyle.BackColor = GridRowAlternate;
    }

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
}
