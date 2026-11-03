using PersonalFinance.Models;
using PersonalFinance.Services;
using PersonalFinance.UI.Dialogs;

namespace PersonalFinance.UI.Views;

public class CategoriesView : UserControl
{
    private readonly IFinanceService _financeService;
    private DataGridView _grid = null!;
    private ComboBox _cboFilter = null!;

    public CategoriesView(IFinanceService financeService)
    {
        _financeService = financeService;
        InitializeComponent();

        _financeService.DataChanged += (s, e) =>
        {
            if (IsHandleCreated && !IsDisposed)
            {
                BeginInvoke(new Action(LoadCategories));
            }
        };

        LoadCategories();
    }

    private void InitializeComponent()
    {
        Font = Theme.FontBody;
        BackColor = Theme.BgApp;
        Dock = DockStyle.Fill;
        Padding = new Padding(24, 20, 24, 24);

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
            Text = "Danh muc Thu chi",
            Font = Theme.FontSectionTitle,
            ForeColor = Theme.TextPrimary,
            AutoSize = true,
            Location = new Point(0, 8)
        };
        pnlTop.Controls.Add(lblPageTitle);

        var lblFilter = new Label { Text = "Phan loai:", Location = new Point(190, 12), AutoSize = true, Font = Theme.FontBodyBold };
        _cboFilter = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(265, 8),
            Width = 130,
            Font = Theme.FontBody
        };
        _cboFilter.Items.Add(new ComboBoxItem("Tat ca", null!));
        _cboFilter.Items.Add(new ComboBoxItem("Chi tieu", TransactionType.Expense));
        _cboFilter.Items.Add(new ComboBoxItem("Thu nhap", TransactionType.Income));
        _cboFilter.SelectedIndex = 0;
        _cboFilter.SelectedIndexChanged += (s, e) => LoadCategories();

        pnlTop.Controls.Add(lblFilter);
        pnlTop.Controls.Add(_cboFilter);

        var btnAdd = new Button
        {
            Text = "+ Them danh muc",
            Size = new Size(130, 32),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(pnlTop.Width - 260, 4)
        };
        Theme.ApplyPrimaryButton(btnAdd);
        btnAdd.Click += OnAddClicked;

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
        pnlTop.Controls.Add(btnEdit);
        pnlTop.Controls.Add(btnDelete);

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
        _grid.Columns.Add("Name", "Ten danh muc");
        _grid.Columns.Add("Type", "Loai danh muc");
        _grid.Columns.Add("Description", "Mo ta");

        if (_grid.Columns["Name"] != null) _grid.Columns["Name"]!.Width = 240;
        if (_grid.Columns["Type"] != null) _grid.Columns["Type"]!.Width = 150;
        if (_grid.Columns["Description"] != null) _grid.Columns["Description"]!.FillWeight = 200;

        _grid.CellDoubleClick += (s, e) => OnEditClicked(s, e);

        pnlGrid.Controls.Add(_grid);

        var pnlSpacer = new Panel { Dock = DockStyle.Top, Height = 14 };

        Controls.Add(pnlGrid);
        Controls.Add(pnlSpacer);
        Controls.Add(pnlTop);
    }

    public void LoadCategories()
    {
        TransactionType? filterType = null;
        if (_cboFilter.SelectedItem is ComboBoxItem item && item.Value is TransactionType t)
        {
            filterType = t;
        }

        var categories = _financeService.GetAllCategories(filterType);
        _grid.Rows.Clear();

        foreach (var cat in categories)
        {
            string typeStr = cat.Type == TransactionType.Income ? "Thu nhap" : "Chi tieu";
            int rowIdx = _grid.Rows.Add(
                cat.Id,
                cat.Name,
                typeStr,
                cat.Description
            );

            var row = _grid.Rows[rowIdx];
            row.Cells["Type"].Style.ForeColor = cat.Type == TransactionType.Income ? Theme.IncomeGreen : Theme.ExpenseRed;
            row.Cells["Type"].Style.Font = Theme.FontBodyBold;
        }
    }

    private void OnAddClicked(object? sender, EventArgs e)
    {
        using var dlg = new CategoryDialog();
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK && dlg.ResultCategory != null)
        {
            _financeService.CreateCategory(dlg.ResultCategory);
        }
    }

    private void OnEditClicked(object? sender, EventArgs e)
    {
        if (_grid.CurrentRow?.Cells["Id"]?.Value is not string id)
        {
            MessageBox.Show("Vui long chon danh muc can sua.", "Thong bao", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var cat = _financeService.GetCategory(id);
        if (cat == null) return;

        using var dlg = new CategoryDialog(cat);
        if (dlg.ShowDialog(FindForm()) == DialogResult.OK && dlg.ResultCategory != null)
        {
            _financeService.UpdateCategory(dlg.ResultCategory);
        }
    }

    private void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (_grid.CurrentRow?.Cells["Id"]?.Value is not string id)
        {
            MessageBox.Show("Vui long chon danh muc can xoa.", "Thong bao", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show("Ban co chac muon xoa danh muc nay?", "Xac nhan", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm == DialogResult.Yes)
        {
            if (!_financeService.DeleteCategory(id, out string errMsg))
            {
                MessageBox.Show(errMsg, "Khong the xoa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
