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
            Text = "Cap nhat tai khoan / vi";
            PopulateData(_editingAccount);
        }
        else
        {
            Text = "Them tai khoan / vi moi";
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
        ClientSize = new Size(440, 360);

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Theme.GridHeaderBg,
            Padding = new Padding(20, 0, 0, 0)
        };
        var lblTitle = new Label
        {
            Text = _editingAccount != null ? "Chinh sua tai khoan / vi" : "Tao moi tai khoan / vi tai chinh",
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
            RowCount = 4,
            Padding = new Padding(24, 20, 24, 10)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        for (int i = 0; i < 4; i++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
        }

        // 1. Ten tai khoan
        var lblName = new Label { Text = "Ten tai khoan:", Anchor = AnchorStyles.Left, AutoSize = true, Font = Theme.FontBodyBold };
        _txtName = new TextBox { Dock = DockStyle.Fill, Font = Theme.FontBody, PlaceholderText = "Vi du: Vietcombank, Vi MoMo..." };

        // 2. Loai tai khoan
        var lblType = new Label { Text = "Loai tai khoan:", Anchor = AnchorStyles.Left, AutoSize = true, Font = Theme.FontBodyBold };
        _cboType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Font = Theme.FontBody };
        _cboType.Items.Add(new ComboBoxItem("Tien mat", AccountType.Cash));
        _cboType.Items.Add(new ComboBoxItem("Tai khoan Ngan hang", AccountType.Bank));
        _cboType.Items.Add(new ComboBoxItem("The tin dung", AccountType.CreditCard));
        _cboType.Items.Add(new ComboBoxItem("Vi dien tu", AccountType.EWallet));
        _cboType.Items.Add(new ComboBoxItem("So tiet kiem", AccountType.Savings));
        _cboType.SelectedIndex = 0;

        // 3. So du ban dau
        var lblInitial = new Label { Text = "So du ban dau:", Anchor = AnchorStyles.Left, AutoSize = true, Font = Theme.FontBodyBold };
        _numInitialBalance = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            Font = Theme.FontBody,
            Minimum = 0,
            Maximum = 1000000000000M,
            Increment = 500000,
            ThousandsSeparator = true
        };

        // 4. Mo ta
        var lblDesc = new Label { Text = "Mo ta / Ghi chu:", Anchor = AnchorStyles.Left, AutoSize = true, Font = Theme.FontBodyBold };
        _txtDescription = new TextBox { Dock = DockStyle.Fill, Font = Theme.FontBody, PlaceholderText = "Mo ta them (khong bat buoc)" };

        layout.Controls.Add(lblName, 0, 0);
        layout.Controls.Add(_txtName, 1, 0);
        layout.Controls.Add(lblType, 0, 1);
        layout.Controls.Add(_cboType, 1, 1);
        layout.Controls.Add(lblInitial, 0, 2);
        layout.Controls.Add(_numInitialBalance, 1, 2);
        layout.Controls.Add(lblDesc, 0, 3);
        layout.Controls.Add(_txtDescription, 1, 3);

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
            Location = new Point(210, 11)
        };
        Theme.ApplySecondaryButton(_btnCancel);
        _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        _btnSave = new Button
        {
            Text = "Luu lai",
            Size = new Size(100, 38),
            Anchor = AnchorStyles.Right,
            Location = new Point(320, 11)
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

    private void PopulateData(Account acc)
    {
        _txtName.Text = acc.Name;
        _numInitialBalance.Value = acc.InitialBalance;
        _numInitialBalance.Enabled = false; // Cannot edit initial balance after creation to avoid mismatch
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
            MessageBox.Show("Vui long nhap ten tai khoan.", "Loi nhap lieu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
