using System.Globalization;

namespace PersonalFinance.UI;

public static class Theme
{
    // Font: 100% Segoe UI chuẩn
    public static readonly Font FontApp = new("Segoe UI", 9.5F, FontStyle.Regular);
    public static readonly Font FontAppBold = new("Segoe UI", 9.5F, FontStyle.Bold);
    public static readonly Font FontHeader = new("Segoe UI", 10.5F, FontStyle.Bold);
    public static readonly Font FontTitle = new("Segoe UI", 13F, FontStyle.Bold);
    public static readonly Font FontStat = new("Segoe UI", 15F, FontStyle.Bold);

    // Bảng màu cơ bản, không màu mè (System & Standard Flat)
    public static readonly Color ColorIncome = Color.FromArgb(21, 128, 61);   // Xanh lá chuẩn (Green 700)
    public static readonly Color ColorExpense = Color.FromArgb(185, 28, 28);  // Đỏ chuẩn (Red 700)
    public static readonly Color ColorTransfer = Color.FromArgb(180, 83, 9); // Nâu cam chuẩn (Amber 700)

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
        grid.BackgroundColor = SystemColors.Window;
        grid.BorderStyle = BorderStyle.Fixed3D;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.Single;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.ReadOnly = true;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.RowTemplate.Height = 28;
        grid.EnableHeadersVisualStyles = true;
        grid.ColumnHeadersHeight = 32;
    }
}
