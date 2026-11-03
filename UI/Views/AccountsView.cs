using PersonalFinance.Models;
using PersonalFinance.Services;
using PersonalFinance.UI.Dialogs;

namespace PersonalFinance.UI.Views;

public class AccountsView : UserControl
{
    private readonly IFinanceService _financeService;
    private DataGridView _grid = null!;
    private Label _lblTotalAssets = null!;

    public AccountsView(IFinanceService financeService)
    {
        _financeService = financeService;
        InitializeComponent();

        _financeService.DataChanged += (s, e) =>
        {
            if (IsHandleCreated && !IsDisposed)
            {
                BeginInvoke(new Action(LoadAccounts));
            }
        };

        LoadAccounts();
    }

    private void InitializeComponent()
    {
        Font = Theme.FontBody;
        BackColor = Theme.BgApp;
        Dock = DockStyle.Fill;
        Padding = new Padding(24, 20, 24, 24);

        // Header Panel
        var pnlTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Theme.BgCard,
            Padding = new Padding(16, 16, 16, 16),
            Margin = new Padding(0, 0, 0, 16)
        };

        var lblPageTitle = new Label
        {
            Text = "Quan ly vi & tai khoan thanh toan",
            Font = Theme.FontSectionTitle,
            ForeColor = Theme.TextPrimary,
            AutoSize = true,
            Location = new Point(0, 8)
        };
        pnlTop.Controls.Add(lblPageTitle);

        var btnAdd = new Button
        {
            Text = "+ Them tai khoan",
            Size = new Size(130, 32),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlTop.Width - 390, 4)
        };
        Theme.ApplyPrimaryButton(btnAdd);
        btnAdd.Click += OnAddClicked;

        var btnTransfer = new Button
        {
            Text = "-> Chuyen tien",
            Size = new Size(120, 32),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlTop.Width - 250, 4)
        };
        Theme.ApplySecondaryButton(btnTransfer);
        btnTransfer.Click += (s, e) =>
        {
            using var dlg = new TransferDialog(_financeService);
            dlg.ShowDialog(FindForm());
        };

        var btnEdit = new Button
        {
            Text = "Sua",
            Size = new Size(60, 32),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlTop.Width - 120, 4)
        };
        Theme.ApplySecondaryButton(btnEdit);
        btnEdit.Click += OnEditClicked;

        var btnDelete = new Button
        {
            Text = "Xoa",
            Size = new Size(60, 32),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlTop.Width - 50, 4)
        };
        Theme.ApplyDangerButton(btnDelete);
        btnDelete.Click += OnDeleteClicked;

        pnlTop.Controls.Add(btnAdd);
        pnlTop.Controls.Add(btnTransfer);
        pnlTop.Controls.Add(btnEdit);
        pnlTop.Controls.Add(btnDelete);

        // Grid Panel
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
        _grid.Columns.Add("Name", "Ten tai khoan / Vi");
        _grid.Columns.Add("Type", "Loai");
        _grid.Columns.Add("InitialBalance", "So du ban dau");
        _grid.Columns.Add("CurrentBalance", "So du hien tai");
        _grid.Columns.Add("Description", "Mo ta");

        if (_grid.Columns["Name"] != null) _grid.Columns["Name"]!.Width = 220;
        if (_grid.Columns["Type"] != null) _grid.Columns["Type"]!.Width = 140;
        if (_grid.Columns["InitialBalance"] != null)
        {
            _grid.Columns["InitialBalance"]!.Width = 160;
            _grid.Columns["InitialBalance"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }
        if (_grid.Columns["CurrentBalance"] != null)
        {
            _grid.Columns["CurrentBalance"]!.Width = 180;
            _grid.Columns["CurrentBalance"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }
        if (_grid.Columns["Description"] != null) _grid.Columns["Description"]!.FillWeight = 200;

        _grid.CellDoubleClick += (s, e) => OnEditClicked(s, e);

        _lblTotalAssets = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 36,
            Font = Theme.FontSectionTitle,
            ForeColor = Theme.Primary,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "Tong so du tat ca vi: 0 VND"
        };

        pnlGrid.Controls.Add(_grid);
        pnlGrid.Controls.Add(_lblTotalAssets);

        var pnlSpacer = new Panel { Dock = DockStyle.Top, Height = 14 };

        Controls.Add(pnlGrid);
        Controls.Add(pnlSpacer);
        Controls.Add(pnlTop);
    }

    public void LoadAccounts()
    {
        var accounts = _financeService.GetAllAccounts();
        _grid.Rows.Clear();
        decimal total = 0;

        foreach (var acc in accounts)
        {
            total += acc.CurrentBalance;
            string typeName = acc.Type switch
            {
                AccountType.Cash => "Tien mat",
                AccountType.Bank => "Ngan hang",
                AccountType.CreditCard => "The tin dung",
                AccountType.EWallet => "Vi dien tu",
                AccountType.Savings => "So tiet kiem",
                _ => acc.Type.ToString()
            };

            int rowIdx = _grid.Rows.Add(
                acc.Id,
                acc.Name,
                typeName,
                Theme.FormatMoney(acc.InitialBalance, acc.Currency),
                Theme.FormatMoney(acc.CurrentBalance, acc.Currency),
                acc.Description
            );

            var row = _grid.Rows[rowIdx];
            row.Cells["CurrentBalance"].Style.Font = Theme.FontBodyBold;
            row.Cells["CurrentBalance"].Style.ForeColor = acc.CurrentBalance >= 0 ? Theme.Primary : Theme.ExpenseRed;
        }

        _lblTotalAssets.Text = $"Tong so du tat ca vi: {Theme.FormatMoney(total)}";
    }

    private void OnAddClicked(object? sender, EventArgs e)
    {
        using var dlg = new AccountDialog();
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK && dlg.ResultAccount != null)
        {
            _financeService.CreateAccount(dlg.ResultAccount);
        }
    }

    private void OnEditClicked(object? sender, EventArgs e)
    {
        if (_grid.CurrentRow?.Cells["Id"]?.Value is not string id)
        {
            MessageBox.Show("Vui long chon tai khoan can sua.", "Thong bao", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var acc = _financeService.GetAccount(id);
        if (acc == null) return;

        using var dlg = new AccountDialog(acc);
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK && dlg.ResultAccount != null)
        {
            _financeService.UpdateAccount(dlg.ResultAccount);
        }
    }

    private void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (_grid.CurrentRow?.Cells["Id"]?.Value is not string id)
        {
            MessageBox.Show("Vui long chon tai khoan can xoa.", "Thong bao", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show("Ban co chac chan muon xoa tai khoan nay khong?", "Xac nhan xoa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm == DialogResult.Yes)
        {
            if (!_financeService.DeleteAccount(id, out string errMsg))
            {
                MessageBox.Show(errMsg, "Khong the xoa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
