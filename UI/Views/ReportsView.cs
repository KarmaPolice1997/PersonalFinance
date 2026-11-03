using PersonalFinance.Models;
using PersonalFinance.Services;

namespace PersonalFinance.UI.Views;

public class ReportsView : UserControl
{
    private readonly IFinanceService _financeService;

    private DateTimePicker _dtpFrom = null!;
    private DateTimePicker _dtpTo = null!;
    private Label _lblIncome = null!;
    private Label _lblExpense = null!;
    private Label _lblSavings = null!;
    private Label _lblSavingsRate = null!;
    private DataGridView _gridExpenses = null!;
    private DataGridView _gridAccounts = null!;

    public ReportsView(IFinanceService financeService)
    {
        _financeService = financeService;
        InitializeComponent();

        _financeService.DataChanged += (s, e) =>
        {
            if (IsHandleCreated && !IsDisposed)
            {
                BeginInvoke(new Action(GenerateReport));
            }
        };

        GenerateReport();
    }

    private void InitializeComponent()
    {
        Font = Theme.FontBody;
        BackColor = Theme.BgApp;
        Dock = DockStyle.Fill;
        AutoScroll = true;
        Padding = new Padding(24, 20, 24, 24);

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        // 1. Period Selector Panel
        var pnlPeriod = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = Theme.BgCard,
            Padding = new Padding(16, 14, 16, 14),
            Margin = new Padding(0, 0, 0, 16)
        };

        var lblTitle = new Label
        {
            Text = "Bao cao tai chinh tong hop",
            Font = Theme.FontSectionTitle,
            ForeColor = Theme.TextPrimary,
            AutoSize = true,
            Location = new Point(0, 8)
        };
        pnlPeriod.Controls.Add(lblTitle);

        var lblFrom = new Label { Text = "Tu ngay:", Location = new Point(270, 12), AutoSize = true, Font = Theme.FontBodyBold };
        _dtpFrom = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            Location = new Point(335, 8),
            Width = 110,
            Font = Theme.FontBody,
            Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
        };
        _dtpFrom.ValueChanged += (s, e) => GenerateReport();

        var lblTo = new Label { Text = "Den ngay:", Location = new Point(460, 12), AutoSize = true, Font = Theme.FontBodyBold };
        _dtpTo = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            Location = new Point(530, 8),
            Width = 110,
            Font = Theme.FontBody,
            Value = DateTime.Today
        };
        _dtpTo.ValueChanged += (s, e) => GenerateReport();

        var btnCurrentMonth = new Button
        {
            Text = "Thang nay",
            Size = new Size(85, 30),
            Location = new Point(660, 7)
        };
        Theme.ApplySecondaryButton(btnCurrentMonth);
        btnCurrentMonth.Click += (s, e) =>
        {
            _dtpFrom.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            _dtpTo.Value = DateTime.Today;
        };

        var btnThisYear = new Button
        {
            Text = "Nam nay",
            Size = new Size(85, 30),
            Location = new Point(755, 7)
        };
        Theme.ApplySecondaryButton(btnThisYear);
        btnThisYear.Click += (s, e) =>
        {
            _dtpFrom.Value = new DateTime(DateTime.Today.Year, 1, 1);
            _dtpTo.Value = DateTime.Today;
        };

        pnlPeriod.Controls.Add(lblFrom);
        pnlPeriod.Controls.Add(_dtpFrom);
        pnlPeriod.Controls.Add(lblTo);
        pnlPeriod.Controls.Add(_dtpTo);
        pnlPeriod.Controls.Add(btnCurrentMonth);
        pnlPeriod.Controls.Add(btnThisYear);
        mainLayout.Controls.Add(pnlPeriod);

        // 2. Summary Cards
        var cardsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 100,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 16)
        };
        for (int i = 0; i < 4; i++)
        {
            cardsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        }

        cardsLayout.Controls.Add(CreateReportCard("TONG THU NHAP KY", out _lblIncome, Theme.IncomeGreen), 0, 0);
        cardsLayout.Controls.Add(CreateReportCard("TONG CHI TIEU KY", out _lblExpense, Theme.ExpenseRed), 1, 0);
        cardsLayout.Controls.Add(CreateReportCard("TIET KIEM THUAN", out _lblSavings, Theme.Primary), 2, 0);
        cardsLayout.Controls.Add(CreateReportCard("TY LE TIET KIEM", out _lblSavingsRate, Theme.TransferAmber), 3, 0);
        mainLayout.Controls.Add(cardsLayout);

        // 3. Tables Layout (Category Breakdown and Account Status)
        var tablesLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 420,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        tablesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        tablesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        // Expense by Category Grid
        var pnlExpenses = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.BgCard,
            Margin = new Padding(0, 0, 8, 0),
            Padding = new Padding(16)
        };
        var lblExpTitle = new Label
        {
            Text = "Chi tieu theo tung danh muc",
            Font = Theme.FontSectionTitle,
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 32
        };
        _gridExpenses = new DataGridView { Dock = DockStyle.Fill };
        Theme.FormatGrid(_gridExpenses);
        _gridExpenses.Columns.Add("Category", "Danh muc chi tieu");
        _gridExpenses.Columns.Add("Amount", "Tong so tien");
        _gridExpenses.Columns.Add("Percent", "Ty trong (%)");
        if (_gridExpenses.Columns["Amount"] != null)
        {
            _gridExpenses.Columns["Amount"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _gridExpenses.Columns["Amount"]!.Width = 140;
        }
        if (_gridExpenses.Columns["Percent"] != null)
        {
            _gridExpenses.Columns["Percent"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _gridExpenses.Columns["Percent"]!.Width = 110;
        }

        pnlExpenses.Controls.Add(_gridExpenses);
        pnlExpenses.Controls.Add(lblExpTitle);
        _gridExpenses.BringToFront();

        // Accounts Status Grid
        var pnlAcc = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.BgCard,
            Margin = new Padding(8, 0, 0, 0),
            Padding = new Padding(16)
        };
        var lblAccTitle = new Label
        {
            Text = "Phan bo so du tren cac tai khoan",
            Font = Theme.FontSectionTitle,
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 32
        };
        _gridAccounts = new DataGridView { Dock = DockStyle.Fill };
        Theme.FormatGrid(_gridAccounts);
        _gridAccounts.Columns.Add("Account", "Ten vi / Tai khoan");
        _gridAccounts.Columns.Add("Balance", "So du hien tai");
        _gridAccounts.Columns.Add("Percent", "Ty trong (%)");
        if (_gridAccounts.Columns["Balance"] != null)
        {
            _gridAccounts.Columns["Balance"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _gridAccounts.Columns["Balance"]!.Width = 150;
        }
        if (_gridAccounts.Columns["Percent"] != null)
        {
            _gridAccounts.Columns["Percent"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _gridAccounts.Columns["Percent"]!.Width = 110;
        }

        pnlAcc.Controls.Add(_gridAccounts);
        pnlAcc.Controls.Add(lblAccTitle);
        _gridAccounts.BringToFront();

        tablesLayout.Controls.Add(pnlExpenses, 0, 0);
        tablesLayout.Controls.Add(pnlAcc, 1, 0);
        mainLayout.Controls.Add(tablesLayout);

        Controls.Add(mainLayout);
    }

    private Control CreateReportCard(string title, out Label valueLabel, Color valueColor)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.BgCard,
            Margin = new Padding(6),
            Padding = new Padding(16, 12, 16, 12)
        };

        var lblTitle = new Label
        {
            Text = title,
            Font = Theme.FontSmall,
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Top,
            Height = 22
        };

        valueLabel = new Label
        {
            Text = "0",
            Font = Theme.FontCardValue,
            ForeColor = valueColor,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        panel.Controls.Add(valueLabel);
        panel.Controls.Add(lblTitle);
        return panel;
    }

    public void GenerateReport()
    {
        DateTime fromDate = _dtpFrom.Value.Date;
        DateTime toDate = _dtpTo.Value.Date.AddDays(1).AddTicks(-1);

        var summary = _financeService.GetSummary(fromDate, toDate);

        _lblIncome.Text = Theme.FormatMoney(summary.TotalIncome);
        _lblExpense.Text = Theme.FormatMoney(summary.TotalExpense);

        decimal savings = summary.NetSavings;
        _lblSavings.Text = Theme.FormatMoney(savings);
        _lblSavings.ForeColor = savings >= 0 ? Theme.Primary : Theme.ExpenseRed;

        decimal rate = summary.TotalIncome > 0 ? (savings / summary.TotalIncome) * 100M : 0;
        _lblSavingsRate.Text = $"{rate:F1}%";

        // Fill Expense Table
        _gridExpenses.Rows.Clear();
        decimal totalExpense = summary.TotalExpense;
        foreach (var item in summary.ExpenseByCategory.OrderByDescending(c => c.Value))
        {
            decimal pct = totalExpense > 0 ? (item.Value / totalExpense) * 100M : 0;
            _gridExpenses.Rows.Add(item.Key, Theme.FormatMoney(item.Value), $"{pct:F1}%");
        }

        // Fill Accounts Table
        _gridAccounts.Rows.Clear();
        decimal totalAssets = summary.TotalAssets;
        foreach (var item in summary.BalanceByAccount.OrderByDescending(a => a.Value))
        {
            decimal pct = totalAssets > 0 ? (item.Value / totalAssets) * 100M : 0;
            _gridAccounts.Rows.Add(item.Key, Theme.FormatMoney(item.Value), $"{pct:F1}%");
        }
    }
}
