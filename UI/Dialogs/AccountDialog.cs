using PersonalFinance.Models;

namespace PersonalFinance.UI.Dialogs;

public class AccountDialog : Form
{
    private readonly Account? _editingAccount;

    private TextBox _txtName = null!;
    private ComboBox _cboType = null!;
    private NumericUpDown _numInitialBalance = null!;
    private TextBox _txtDescription = null!;
    private Button _btnSave = null!;
    private Button _btnCancel = null!;

    public Account? ResultAccount { get; private set; }

    public AccountDialog(Account? accountToEdit = null)
    {
        _editingAccount = accountToEdit;
        InitializeComponent();

        if (_editingAccount != null)
        {
            Text = "Cập nhật tài khoản / ví";
            PopulateData(_editingAccount);
        }
        else
        {
            Text = "Thêm tài khoản / ví mới";
        }
    }

    private void InitializeComponent()
    {
        Font = Theme.FontApp;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(420, 240);

        var grp = new GroupBox
        {
            Text = "Thông tin tài khoản ví",
            Dock = DockStyle.Top,
            Height = 180,
            Location = new Point(12, 10),
            Padding = new Padding(12)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(8)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        for (int i = 0; i < 4; i++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        }

        var lblName = new Label { Text = "Tên tài khoản / Ví:", Anchor = AnchorStyles.Left, AutoSize = true };
        _txtName = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Ví dụ: Vietcombank, Tiền mặt..." };

        var lblType = new Label { Text = "Loại tài khoản:", Anchor = AnchorStyles.Left, AutoSize = true };
        _cboType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
        _cboType.Items.Add(new ComboBoxItem("Tiền mặt", AccountType.Cash));
        _cboType.Items.Add(new ComboBoxItem("Tài khoản ngân hàng", AccountType.Bank));
        _cboType.Items.Add(new ComboBoxItem("Thẻ tín dụng", AccountType.CreditCard));
        _cboType.Items.Add(new ComboBoxItem("Ví điện tử", AccountType.EWallet));
        _cboType.Items.Add(new ComboBoxItem("Sổ tiết kiệm", AccountType.Savings));
        _cboType.SelectedIndex = 0;

        var lblInitial = new Label { Text = "Số dư ban đầu:", Anchor = AnchorStyles.Left, AutoSize = true };
        _numInitialBalance = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            Minimum = 0,
            Maximum = 1000000000000M,
            Increment = 500000,
            ThousandsSeparator = true
        };

        var lblDesc = new Label { Text = "Mô tả / Ghi chú:", Anchor = AnchorStyles.Left, AutoSize = true };
        _txtDescription = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Ghi chú thêm (tùy chọn)" };

        layout.Controls.Add(lblName, 0, 0);
        layout.Controls.Add(_txtName, 1, 0);
        layout.Controls.Add(lblType, 0, 1);
        layout.Controls.Add(_cboType, 1, 1);
        layout.Controls.Add(lblInitial, 0, 2);
        layout.Controls.Add(_numInitialBalance, 1, 2);
        layout.Controls.Add(lblDesc, 0, 3);
        layout.Controls.Add(_txtDescription, 1, 3);

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
            Location = new Point(225, 6)
        };
        _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        _btnSave = new Button
        {
            Text = "Lưu lại",
            Size = new Size(85, 30),
            Anchor = AnchorStyles.Right,
            Location = new Point(318, 6)
        };
        _btnSave.Click += OnSaveClicked;

        pnlButtons.Controls.Add(_btnCancel);
        pnlButtons.Controls.Add(_btnSave);

        Controls.Add(grp);
        Controls.Add(pnlButtons);

        AcceptButton = _btnSave;
        CancelButton = _btnCancel;

        Theme.ApplyTheme(this);
    }

    private void PopulateData(Account acc)
    {
        _txtName.Text = acc.Name;
        _numInitialBalance.Value = acc.InitialBalance;
        _numInitialBalance.Enabled = false;
        _txtDescription.Text = acc.Description;

        for (int i = 0; i < _cboType.Items.Count; i++)
        {
            if (_cboType.Items[i] is ComboBoxItem item && (AccountType)item.Value == acc.Type)
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
            MessageBox.Show("Vui lòng nhập tên tài khoản/ví.", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtName.Focus();
            return;
        }

        var accType = AccountType.Cash;
        if (_cboType.SelectedItem is ComboBoxItem item && item.Value is AccountType t)
        {
            accType = t;
        }

        ResultAccount = new Account
        {
            Id = _editingAccount?.Id ?? Guid.NewGuid().ToString("N"),
            Name = name,
            Type = accType,
            InitialBalance = _editingAccount?.InitialBalance ?? _numInitialBalance.Value,
            CurrentBalance = _editingAccount?.CurrentBalance ?? _numInitialBalance.Value,
            Description = _txtDescription.Text.Trim(),
            CreatedAt = _editingAccount?.CreatedAt ?? DateTime.Now,
            IsActive = true
        };

        DialogResult = DialogResult.OK;
        Close();
    }
}
