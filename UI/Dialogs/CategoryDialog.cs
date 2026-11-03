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
            Text = "Cập nhật danh mục";
            PopulateData(_editingCategory);
        }
        else
        {
            Text = "Thêm danh mục mới";
        }
    }

    private void InitializeComponent()
    {
        Font = Theme.FontApp;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(400, 200);

        var grp = new GroupBox
        {
            Text = "Thông tin danh mục thu chi",
            Dock = DockStyle.Top,
            Height = 140,
            Location = new Point(12, 10),
            Padding = new Padding(12)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(8)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        for (int i = 0; i < 3; i++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        }

        var lblName = new Label { Text = "Tên danh mục:", Anchor = AnchorStyles.Left, AutoSize = true };
        _txtName = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Ví dụ: Ăn uống, Tiền lương..." };

        var lblType = new Label { Text = "Loại danh mục:", Anchor = AnchorStyles.Left, AutoSize = true };
        _cboType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
        _cboType.Items.Add(new ComboBoxItem("Chi tiêu", TransactionType.Expense));
        _cboType.Items.Add(new ComboBoxItem("Thu nhập", TransactionType.Income));
        _cboType.SelectedIndex = 0;

        var lblDesc = new Label { Text = "Mô tả / Ghi chú:", Anchor = AnchorStyles.Left, AutoSize = true };
        _txtDescription = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Ghi chú thêm (tùy chọn)" };

        layout.Controls.Add(lblName, 0, 0);
        layout.Controls.Add(_txtName, 1, 0);
        layout.Controls.Add(lblType, 0, 1);
        layout.Controls.Add(_cboType, 1, 1);
        layout.Controls.Add(lblDesc, 0, 2);
        layout.Controls.Add(_txtDescription, 1, 2);

        grp.Controls.Add(layout);

        var pnlButtons = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 45,
            Padding = new Padding(0, 0, 12, 10)
        };

        _btnCancel = new Button
        {
            Text = "Hủy bỏ",
            Size = new Size(85, 30),
            Anchor = AnchorStyles.Right,
            Location = new Point(205, 6)
        };
        _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        _btnSave = new Button
        {
            Text = "Lưu lại",
            Size = new Size(85, 30),
            Anchor = AnchorStyles.Right,
            Location = new Point(298, 6)
        };
        _btnSave.Click += OnSaveClicked;

        pnlButtons.Controls.Add(_btnCancel);
        pnlButtons.Controls.Add(_btnSave);

        Controls.Add(grp);
        Controls.Add(pnlButtons);

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
            MessageBox.Show("Vui lòng nhập tên danh mục.", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
