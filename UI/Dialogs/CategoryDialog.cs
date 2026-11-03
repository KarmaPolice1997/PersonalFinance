using PersonalFinance.Models;

namespace PersonalFinance.UI.Dialogs;

public class CategoryDialog : Form
{
    private readonly Category? _editingCategory;

    private TextBox _txtName = null!;
    private ComboBox _cboType = null!;
    private TextBox _txtDescription = null!;
    private Button _btnSave = null!;
    private Button _btnCancel = null!;

    public Category? ResultCategory { get; private set; }

    public CategoryDialog(Category? categoryToEdit = null)
    {
        _editingCategory = categoryToEdit;
        InitializeComponent();

        if (_editingCategory != null)
        {
            Text = "Cap nhat danh muc";
            PopulateData(_editingCategory);
        }
        else
        {
            Text = "Them danh muc moi";
        }
    }

    private void InitializeComponent()
    {
        Font = Theme.FontBody;
        BackColor = Theme.BgCard;
        ForeColor = Theme.TextPrimary;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(420, 310);

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Theme.GridHeaderBg,
            Padding = new Padding(20, 0, 0, 0)
        };
        var lblTitle = new Label
        {
            Text = _editingCategory != null ? "Chinh sua danh muc thu chi" : "Tao danh muc thu chi moi",
            Font = Theme.FontSectionTitle,
            ForeColor = Theme.TextPrimary,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        pnlHeader.Controls.Add(lblTitle);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(24, 20, 24, 10)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        for (int i = 0; i < 3; i++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
        }

        var lblName = new Label { Text = "Ten danh muc:", Anchor = AnchorStyles.Left, AutoSize = true, Font = Theme.FontBodyBold };
        _txtName = new TextBox { Dock = DockStyle.Fill, Font = Theme.FontBody, PlaceholderText = "Vi du: An uong, Tien luong..." };

        var lblType = new Label { Text = "Loai danh muc:", Anchor = AnchorStyles.Left, AutoSize = true, Font = Theme.FontBodyBold };
        _cboType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Font = Theme.FontBody };
        _cboType.Items.Add(new ComboBoxItem("Chi tieu", TransactionType.Expense));
        _cboType.Items.Add(new ComboBoxItem("Thu nhap", TransactionType.Income));
        _cboType.SelectedIndex = 0;

        var lblDesc = new Label { Text = "Mo ta:", Anchor = AnchorStyles.Left, AutoSize = true, Font = Theme.FontBodyBold };
        _txtDescription = new TextBox { Dock = DockStyle.Fill, Font = Theme.FontBody, PlaceholderText = "Mo ta them (tuy chon)" };

        layout.Controls.Add(lblName, 0, 0);
        layout.Controls.Add(_txtName, 1, 0);
        layout.Controls.Add(lblType, 0, 1);
        layout.Controls.Add(_cboType, 1, 1);
        layout.Controls.Add(lblDesc, 0, 2);
        layout.Controls.Add(_txtDescription, 1, 2);

        var pnlFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Theme.BgApp,
            Padding = new Padding(0, 0, 20, 0)
        };
        _btnCancel = new Button
        {
            Text = "Huy bo",
            Size = new Size(100, 38),
            Anchor = AnchorStyles.Right,
            Location = new Point(190, 11)
        };
        Theme.ApplySecondaryButton(_btnCancel);
        _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        _btnSave = new Button
        {
            Text = "Luu lai",
            Size = new Size(100, 38),
            Anchor = AnchorStyles.Right,
            Location = new Point(300, 11)
        };
        Theme.ApplyPrimaryButton(_btnSave);
        _btnSave.Click += OnSaveClicked;

        pnlFooter.Controls.Add(_btnCancel);
        pnlFooter.Controls.Add(_btnSave);

        Controls.Add(layout);
        Controls.Add(pnlFooter);
        Controls.Add(pnlHeader);

        AcceptButton = _btnSave;
        CancelButton = _btnCancel;
    }

    private void PopulateData(Category cat)
    {
        _txtName.Text = cat.Name;
        _txtDescription.Text = cat.Description;
        for (int i = 0; i < _cboType.Items.Count; i++)
        {
            if (_cboType.Items[i] is ComboBoxItem item && (TransactionType)item.Value == cat.Type)
            {
                _cboType.SelectedIndex = i;
                break;
            }
        }
    }

    private void OnSaveClicked(object? sender, EventArgs e)
    {
        var name = _txtName.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Vui long nhap ten danh muc.", "Loi nhap lieu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtName.Focus();
            return;
        }

        var catType = TransactionType.Expense;
        if (_cboType.SelectedItem is ComboBoxItem item && item.Value is TransactionType t)
        {
            catType = t;
        }

        ResultCategory = new Category
        {
            Id = _editingCategory?.Id ?? Guid.NewGuid().ToString("N"),
            Name = name,
            Type = catType,
            Description = _txtDescription.Text.Trim()
        };

        DialogResult = DialogResult.OK;
        Close();
    }
}
