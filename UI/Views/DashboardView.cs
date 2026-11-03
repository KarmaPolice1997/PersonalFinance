using PersonalFinance.Models;
using PersonalFinance.Services;
using PersonalFinance.UI.Dialogs;

namespace PersonalFinance.UI.Views;

public class DashboardView : UserControl
{
    private readonly IFinanceService _financeService;

    private Label _lblAssetValue = null!;
    private Label _lblIncomeValue = null!;
    private Label _lblExpenseValue = null!;
    private Label _lblSavingsValue = null!;

    private DataGridView _gridRecent = null!;
    private Panel _pnlCategoryBars = null!;

    public DashboardView(IFinanceService financeService)
    {
        _financeService = financeService;
        InitializeComponent();

        _financeService.DataChanged += (s, e) =>
        {
            if (IsHandleCreated && !IsDisposed)
            {
                BeginInvoke(new Action(LoadDashboardData));
            }
        };

        LoadDashboardData();
    }

    private void InitializeComponent()
    {
        Font = Theme.FontBody;
        BackColor = Theme.BgApp;
        Dock = DockStyle.Fill;
        AutoScroll = true;

        // Container Table
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            Padding = new Padding(24, 20, 24, 24)
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        // 1. Metric Cards Row
        var cardsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 110,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 16)
        };
        for (int i = 0; i < 4; i++)
        {
            cardsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        }

        var card1 = CreateMetricCard("TONG TAI SAN KHA DUNG", out _lblAssetValue, Theme.TextPrimary);
        var card2 = CreateMetricCard("TONG THU NHAP", out _lblIncomeValue, Theme.IncomeGreen);
        var card3 = CreateMetricCard("TONG CHI TIEU", out _lblExpenseValue, Theme.ExpenseRed);
        var card4 = CreateMetricCard("TIET KIEM THUAN", out _lblSavingsValue, Theme.Primary);

        cardsLayout.Controls.Add(card1, 0, 0);
        cardsLayout.Controls.Add(card2, 1, 0);
        cardsLayout.Controls.Add(card3, 2, 0);
        cardsLayout.Controls.Add(card4, 3, 0);
        mainLayout.Controls.Add(cardsLayout);

        // 2. Quick Action Bar
        var pnlActions = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            Margin = new Padding(0, 0, 0, 16),
            BackColor = Theme.BgCard
        };

        var lblQuick = new Label
        {
            Text = "Thao tac nhanh:",
            Font = Theme.FontBodyBold,
            ForeColor = Theme.TextPrimary,
            Location = new Point(16, 15),
            AutoSize = true
        };
        pnlActions.Controls.Add(lblQuick);

        var btnAddExpense = new Button
        {
            Text = "+ Ghi chi tieu",
            Size = new Size(130, 32),
            Location = new Point(140, 9)
        };
        Theme.ApplyDangerButton(btnAddExpense);
        btnAddExpense.Click += (s, e) => OpenQuickTransaction(TransactionType.Expense);
        pnlActions.Controls.Add(btnAddExpense);

        var btnAddIncome = new Button
        {
            Text = "+ Ghi thu nhap",
            Size = new Size(130, 32),
            Location = new Point(280, 9)
        };
        Theme.ApplyPrimaryButton(btnAddIncome);
        btnAddIncome.BackColor = Theme.IncomeGreen;
        btnAddIncome.Click += (s, e) => OpenQuickTransaction(TransactionType.Income);
        pnlActions.Controls.Add(btnAddIncome);

        var btnTransfer = new Button
        {
            Text = "-> Chuyen khoan",
            Size = new Size(140, 32),
            Location = new Point(420, 9)
        };
        Theme.ApplySecondaryButton(btnTransfer);
        btnTransfer.Click += (s, e) =>
        {
            using var dlg = new TransferDialog(_financeService);
            dlg.ShowDialog(FindForm());
        };
        pnlActions.Controls.Add(btnTransfer);

        mainLayout.Controls.Add(pnlActions);

        // 3. Lower Content: Recent Transactions (Left) + Category Breakdown (Right)
        var contentLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 440,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
        contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

        // Left Panel: Recent Transactions
        var pnlRecent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.BgCard,
            Margin = new Padding(0, 0, 10, 0),
            Padding = new Padding(16)
        };
        var lblRecentTitle = new Label
        {
            Text = "Giao dich gan day",
            Font = Theme.FontSectionTitle,
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 32
        };
        pnlRecent.Controls.Add(lblRecentTitle);

        _gridRecent = new DataGridView
        {
            Dock = DockStyle.Fill
        };
        Theme.FormatGrid(_gridRecent);
        _gridRecent.Columns.Add("Date", "Ngay");
        _gridRecent.Columns.Add("Type", "Loai");
        _gridRecent.Columns.Add("Category", "Danh muc");
        _gridRecent.Columns.Add("Account", "Tai khoan");
        _gridRecent.Columns.Add("Amount", "So tien");

        var colDate = _gridRecent.Columns["Date"];
        if (colDate != null) colDate.Width = 85;
        var colType = _gridRecent.Columns["Type"];
        if (colType != null) colType.Width = 90;
        var colAmount = _gridRecent.Columns["Amount"];
        if (colAmount != null) colAmount.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

        pnlRecent.Controls.Add(_gridRecent);
        _gridRecent.BringToFront();

        // Right Panel: Categories
        var pnlCat = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.BgCard,
            Margin = new Padding(10, 0, 0, 0),
            Padding = new Padding(16)
        };
        var lblCatTitle = new Label
        {
            Text = "Co cau chi tieu theo danh muc",
            Font = Theme.FontSectionTitle,
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 32
        };
        pnlCat.Controls.Add(lblCatTitle);

        _pnlCategoryBars = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true
        };
        pnlCat.Controls.Add(_pnlCategoryBars);
        _pnlCategoryBars.BringToFront();

        contentLayout.Controls.Add(pnlRecent, 0, 0);
        contentLayout.Controls.Add(pnlCat, 1, 0);

        mainLayout.Controls.Add(contentLayout);
        Controls.Add(mainLayout);
    }

    private Control CreateMetricCard(string title, out Label valueLabel, Color valueColor)
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
            Text = "0 VND",
            Font = Theme.FontCardValue,
            ForeColor = valueColor,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        panel.Controls.Add(valueLabel);
        panel.Controls.Add(lblTitle);
        return panel;
    }

    private void OpenQuickTransaction(TransactionType type)
    {
        using var dlg = new TransactionDialog(_financeService);
        // Find and select type in dialog if default
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK && dlg.ResultTransaction != null)
        {
            _financeService.AddTransaction(dlg.ResultTransaction);
        }
    }

    public void LoadDashboardData()
    {
        var summary = _financeService.GetSummary();
        _lblAssetValue.Text = Theme.FormatMoney(summary.TotalAssets);
        _lblIncomeValue.Text = $"+{Theme.FormatMoney(summary.TotalIncome)}";
        _lblExpenseValue.Text = $"-{Theme.FormatMoney(summary.TotalExpense)}";

        decimal netSavings = summary.NetSavings;
        _lblSavingsValue.Text = (netSavings >= 0 ? "+" : "") + Theme.FormatMoney(netSavings);
        _lblSavingsValue.ForeColor = netSavings >= 0 ? Theme.Primary : Theme.ExpenseRed;

        // Load Recent Transactions (Top 8)
        var recent = _financeService.GetTransactions().Take(8).ToList();
        var accounts = _financeService.GetAllAccounts().ToDictionary(a => a.Id, a => a.Name);
        var categories = _financeService.GetAllCategories().ToDictionary(c => c.Id, c => c.Name);

        _gridRecent.Rows.Clear();
        foreach (var tx in recent)
        {
            string typeStr = tx.Type switch
            {
                TransactionType.Income => "[Thu]",
                TransactionType.Expense => "[Chi]",
                TransactionType.Transfer => "[Chuyen]",
                _ => ""
            };

            string catName = tx.CategoryId != null && categories.TryGetValue(tx.CategoryId, out var cn) ? cn : "-";
            string accName = tx.SourceAccountId != null && accounts.TryGetValue(tx.SourceAccountId, out var an) ? an : "-";
            if (tx.Type == TransactionType.Transfer && tx.DestinationAccountId != null && accounts.TryGetValue(tx.DestinationAccountId, out var dn))
            {
                accName = $"{accName} -> {dn}";
            }

            int rowIdx = _gridRecent.Rows.Add(
                Theme.FormatDate(tx.TransactionDate),
                typeStr,
                catName,
                accName,
                Theme.FormatMoney(tx.Amount)
            );

            var row = _gridRecent.Rows[rowIdx];
            if (tx.Type == TransactionType.Income)
            {
                row.Cells["Amount"].Style.ForeColor = Theme.IncomeGreen;
                row.Cells["Type"].Style.ForeColor = Theme.IncomeGreen;
            }
            else if (tx.Type == TransactionType.Expense)
            {
                row.Cells["Amount"].Style.ForeColor = Theme.ExpenseRed;
                row.Cells["Type"].Style.ForeColor = Theme.ExpenseRed;
            }
            else
            {
                row.Cells["Amount"].Style.ForeColor = Theme.TransferAmber;
                row.Cells["Type"].Style.ForeColor = Theme.TransferAmber;
            }
        }

        // Render Category Breakdown Bars
        RenderCategoryBreakdown(summary);
    }

    private void RenderCategoryBreakdown(FinancialSummary summary)
    {
        _pnlCategoryBars.Controls.Clear();
        decimal totalExpense = summary.TotalExpense;

        if (totalExpense <= 0 || summary.ExpenseByCategory.Count == 0)
        {
            var lblEmpty = new Label
            {
                Text = "Chua co du lieu chi tieu.",
                Font = Theme.FontBody,
                ForeColor = Theme.TextMuted,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _pnlCategoryBars.Controls.Add(lblEmpty);
            return;
        }

        int top = 8;
        var sortedCats = summary.ExpenseByCategory.OrderByDescending(c => c.Value).Take(6);

        foreach (var item in sortedCats)
        {
            decimal pct = (item.Value / totalExpense) * 100M;

            var pnlRow = new Panel
            {
                Location = new Point(0, top),
                Size = new Size(_pnlCategoryBars.Width - 10, 48),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var lblName = new Label
            {
                Text = item.Key,
                Font = Theme.FontBodyBold,
                ForeColor = Theme.TextPrimary,
                Location = new Point(0, 0),
                AutoSize = true
            };

            var lblAmount = new Label
            {
                Text = $"{Theme.FormatMoney(item.Value)} ({pct:F1}%)",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextSecondary,
                Location = new Point(pnlRow.Width - 170, 0),
                Size = new Size(170, 18),
                TextAlign = ContentAlignment.TopRight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            // Custom flat solid bar
            var barBg = new Panel
            {
                Location = new Point(0, 22),
                Size = new Size(pnlRow.Width - 10, 10),
                BackColor = Theme.BgApp,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            int fillWidth = Math.Max(4, (int)((barBg.Width * (double)pct) / 100.0));
            var barFill = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(fillWidth, 10),
                BackColor = Theme.ExpenseRed
            };
            barBg.Controls.Add(barFill);

            pnlRow.Controls.Add(lblName);
            pnlRow.Controls.Add(lblAmount);
            pnlRow.Controls.Add(barBg);

            _pnlCategoryBars.Controls.Add(pnlRow);
            top += 52;
        }
    }
}
