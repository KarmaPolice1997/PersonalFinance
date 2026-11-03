using PersonalFinance.Models;
using PersonalFinance.Services;
using PersonalFinance.UI.Dialogs;

namespace PersonalFinance.UI.Views;

public class TransactionsView : UserControl
{
    private readonly IFinanceService _financeService;

    private DataGridView _grid = null!;
    private TextBox _txtSearch = null!;
    private ComboBox _cboFilterType = null!;
    private ComboBox _cboFilterAccount = null!;
    private DateTimePicker _dtpFrom = null!;
    private DateTimePicker _dtpTo = null!;
    private Label _lblStatusSummary = null!;

    public TransactionsView(IFinanceService financeService)
    {
        _financeService = financeService;
        InitializeComponent();

        _financeService.DataChanged += (s, e) =>
        {
            if (IsHandleCreated && !IsDisposed)
            {
                BeginInvoke(new Action(LoadTransactions));
            }
        };

        LoadFilters();
        LoadTransactions();
    }

    private void InitializeComponent()
    {
        Font = Theme.FontBody;
        BackColor = Theme.BgApp;
        Dock = DockStyle.Fill;
        Padding = new Padding(24, 20, 24, 24);

        // Header and Toolbar Panel
        var pnlTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 120,
            BackColor = Theme.BgCard,
            Padding = new Padding(16, 12, 16, 12),
            Margin = new Padding(0, 0, 0, 16)
        };

        // Actions Row (Top of pnlTop)
        var pnlActions = new Panel
        {
            Dock = DockStyle.Top,
            Height = 40
        };

        var lblPageTitle = new Label
        {
            Text = "Danh sach tat ca giao dich",
            Font = Theme.FontSectionTitle,
            ForeColor = Theme.TextPrimary,
            AutoSize = true,
            Location = new Point(0, 8)
        };
        pnlActions.Controls.Add(lblPageTitle);

        var btnAdd = new Button
        {
            Text = "+ Them giao dich",
            Size = new Size(130, 32),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlTop.Width - 430, 4)
        };
        Theme.ApplyPrimaryButton(btnAdd);
        btnAdd.Click += OnAddClicked;

        var btnEdit = new Button
        {
            Text = "Chinh sua",
            Size = new Size(90, 32),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlTop.Width - 290, 4)
        };
        Theme.ApplySecondaryButton(btnEdit);
        btnEdit.Click += OnEditClicked;

        var btnDelete = new Button
        {
            Text = "Xoa",
            Size = new Size(80, 32),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlTop.Width - 190, 4)
        };
        Theme.ApplyDangerButton(btnDelete);
        btnDelete.Click += OnDeleteClicked;

        var btnReset = new Button
        {
            Text = "Lam moi",
            Size = new Size(90, 32),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlTop.Width - 100, 4)
        };
        Theme.ApplySecondaryButton(btnReset);
        btnReset.Click += (s, e) => ResetFilters();

        pnlActions.Controls.Add(btnAdd);
        pnlActions.Controls.Add(btnEdit);
        pnlActions.Controls.Add(btnDelete);
        pnlActions.Controls.Add(btnReset);
        pnlTop.Controls.Add(pnlActions);

        // Filter Controls Row
        var pnlFilters = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 50
        };

        var lblFilterType = new Label { Text = "Loai:", Location = new Point(0, 16), AutoSize = true, Font = Theme.FontBodyBold };
        _cboFilterType = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(45, 12),
            Width = 110,
            Font = Theme.FontBody
        };
        _cboFilterType.Items.Add(new ComboBoxItem("Tat ca", null!));
        _cboFilterType.Items.Add(new ComboBoxItem("Chi tieu", TransactionType.Expense));
        _cboFilterType.Items.Add(new ComboBoxItem("Thu nhap", TransactionType.Income));
        _cboFilterType.Items.Add(new ComboBoxItem("Chuyen khoan", TransactionType.Transfer));
        _cboFilterType.SelectedIndex = 0;
        _cboFilterType.SelectedIndexChanged += (s, e) => LoadTransactions();

        var lblFilterAcc = new Label { Text = "Vi:", Location = new Point(170, 16), AutoSize = true, Font = Theme.FontBodyBold };
        _cboFilterAccount = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(195, 12),
            Width = 150,
            Font = Theme.FontBody
        };
        _cboFilterAccount.SelectedIndexChanged += (s, e) => LoadTransactions();

        var lblDateFrom = new Label { Text = "Tu:", Location = new Point(360, 16), AutoSize = true, Font = Theme.FontBodyBold };
        _dtpFrom = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            Location = new Point(390, 12),
            Width = 105,
            Font = Theme.FontBody,
            Value = DateTime.Today.AddMonths(-1)
        };
        _dtpFrom.ValueChanged += (s, e) => LoadTransactions();

        var lblDateTo = new Label { Text = "Den:", Location = new Point(510, 16), AutoSize = true, Font = Theme.FontBodyBold };
        _dtpTo = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            Location = new Point(550, 12),
            Width = 105,
            Font = Theme.FontBody,
            Value = DateTime.Today
        };
        _dtpTo.ValueChanged += (s, e) => LoadTransactions();

        _txtSearch = new TextBox
        {
            PlaceholderText = "Tim kiem ghi chu...",
            Location = new Point(675, 12),
            Width = 180,
            Font = Theme.FontBody
        };
        _txtSearch.TextChanged += (s, e) => LoadTransactions();

        pnlFilters.Controls.Add(lblFilterType);
        pnlFilters.Controls.Add(_cboFilterType);
        pnlFilters.Controls.Add(lblFilterAcc);
        pnlFilters.Controls.Add(_cboFilterAccount);
        pnlFilters.Controls.Add(lblDateFrom);
        pnlFilters.Controls.Add(_dtpFrom);
        pnlFilters.Controls.Add(lblDateTo);
        pnlFilters.Controls.Add(_dtpTo);
        pnlFilters.Controls.Add(_txtSearch);

        pnlTop.Controls.Add(pnlFilters);

        // Grid Container Panel
        var pnlGrid = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.BgCard,
            Padding = new Padding(16)
        };

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill
        };
        Theme.FormatGrid(_grid);
        _grid.Columns.Add("Id", "ID");
        if (_grid.Columns["Id"] != null) _grid.Columns["Id"]!.Visible = false;
        _grid.Columns.Add("Date", "Ngay");
        _grid.Columns.Add("Type", "Loai");
        _grid.Columns.Add("Category", "Danh muc");
        _grid.Columns.Add("SourceAccount", "Tai khoan nguon");
        _grid.Columns.Add("DestAccount", "Tai khoan dich");
        _grid.Columns.Add("Note", "Ghi chu");
        _grid.Columns.Add("Amount", "So tien");

        if (_grid.Columns["Date"] != null) _grid.Columns["Date"]!.Width = 95;
        if (_grid.Columns["Type"] != null) _grid.Columns["Type"]!.Width = 100;
        if (_grid.Columns["Category"] != null) _grid.Columns["Category"]!.Width = 140;
        if (_grid.Columns["SourceAccount"] != null) _grid.Columns["SourceAccount"]!.Width = 150;
        if (_grid.Columns["DestAccount"] != null) _grid.Columns["DestAccount"]!.Width = 150;
        if (_grid.Columns["Note"] != null) _grid.Columns["Note"]!.FillWeight = 200;
        if (_grid.Columns["Amount"] != null)
        {
            _grid.Columns["Amount"]!.Width = 140;
            _grid.Columns["Amount"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        _grid.CellDoubleClick += (s, e) => OnEditClicked(s, e);

        // Status Summary at bottom of grid
        _lblStatusSummary = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 32,
            Font = Theme.FontBodyBold,
            ForeColor = Theme.TextSecondary,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "Tong so giao dich: 0"
        };

        pnlGrid.Controls.Add(_grid);
        pnlGrid.Controls.Add(_lblStatusSummary);

        // Spacer between Top and Grid
        var pnlSpacer = new Panel { Dock = DockStyle.Top, Height = 14 };

        Controls.Add(pnlGrid);
        Controls.Add(pnlSpacer);
        Controls.Add(pnlTop);
    }

    private void LoadFilters()
    {
        _cboFilterAccount.Items.Clear();
        _cboFilterAccount.Items.Add(new ComboBoxItem("Tat ca vi", null!));

        var accounts = _financeService.GetAllAccounts();
        foreach (var acc in accounts)
        {
            _cboFilterAccount.Items.Add(new ComboBoxItem(acc.Name, acc.Id));
        }

        if (_cboFilterAccount.Items.Count > 0)
            _cboFilterAccount.SelectedIndex = 0;
    }

    private void ResetFilters()
    {
        _cboFilterType.SelectedIndex = 0;
        _cboFilterAccount.SelectedIndex = 0;
        _dtpFrom.Value = DateTime.Today.AddMonths(-1);
        _dtpTo.Value = DateTime.Today;
        _txtSearch.Text = string.Empty;
        LoadTransactions();
    }

    public void LoadTransactions()
    {
        TransactionType? filterType = null;
        if (_cboFilterType.SelectedItem is ComboBoxItem typeItem && typeItem.Value is TransactionType t)
        {
            filterType = t;
        }

        string? accountId = null;
        if (_cboFilterAccount.SelectedItem is ComboBoxItem accItem && accItem.Value is string aid)
        {
            accountId = aid;
        }

        DateTime fromDate = _dtpFrom.Value.Date;
        DateTime toDate = _dtpTo.Value.Date.AddDays(1).AddTicks(-1);
        string search = _txtSearch.Text;

        var list = _financeService.GetTransactions(fromDate, toDate, accountId, null, filterType, search);
        var accounts = _financeService.GetAllAccounts().ToDictionary(a => a.Id, a => a.Name);
        var categories = _financeService.GetAllCategories().ToDictionary(c => c.Id, c => c.Name);

        _grid.Rows.Clear();
        decimal totalIncome = 0;
        decimal totalExpense = 0;

        foreach (var tx in list)
        {
            string typeStr = tx.Type switch
            {
                TransactionType.Income => "[Thu nhap]",
                TransactionType.Expense => "[Chi tieu]",
                TransactionType.Transfer => "[Chuyen khoan]",
                _ => ""
            };

            string catName = tx.CategoryId != null && categories.TryGetValue(tx.CategoryId, out var cn) ? cn : "-";
            string srcAcc = tx.SourceAccountId != null && accounts.TryGetValue(tx.SourceAccountId, out var sn) ? sn : "-";
            string dstAcc = tx.DestinationAccountId != null && accounts.TryGetValue(tx.DestinationAccountId, out var dn) ? dn : "-";

            if (tx.Type == TransactionType.Income) totalIncome += tx.Amount;
            if (tx.Type == TransactionType.Expense) totalExpense += tx.Amount;

            int rowIdx = _grid.Rows.Add(
                tx.Id,
                Theme.FormatDate(tx.TransactionDate),
                typeStr,
                catName,
                srcAcc,
                dstAcc,
                tx.Note,
                Theme.FormatMoney(tx.Amount)
            );

            var row = _grid.Rows[rowIdx];
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

        _lblStatusSummary.Text = $"Tong giao dich: {list.Count} | Thu: +{Theme.FormatMoney(totalIncome)} | Chi: -{Theme.FormatMoney(totalExpense)}";
    }

    private void OnAddClicked(object? sender, EventArgs e)
    {
        using var dlg = new TransactionDialog(_financeService);
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK && dlg.ResultTransaction != null)
        {
            _financeService.AddTransaction(dlg.ResultTransaction);
        }
    }

    private void OnEditClicked(object? sender, EventArgs e)
    {
        if (_grid.CurrentRow?.Cells["Id"]?.Value is not string id)
        {
            MessageBox.Show("Vui long chon mot giao dich de sua.", "Thong bao", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var tx = _financeService.GetTransactions().FirstOrDefault(t => t.Id == id);
        if (tx == null) return;

        using var dlg = new TransactionDialog(_financeService, tx);
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK && dlg.ResultTransaction != null)
        {
            _financeService.UpdateTransaction(tx, dlg.ResultTransaction);
        }
    }

    private void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (_grid.CurrentRow?.Cells["Id"]?.Value is not string id)
        {
            MessageBox.Show("Vui long chon mot giao dich can xoa.", "Thong bao", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show("Ban co chac chan muon xoa giao dich nay khong?", "Xac nhan xoa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm == DialogResult.Yes)
        {
            _financeService.DeleteTransaction(id);
        }
    }
}
