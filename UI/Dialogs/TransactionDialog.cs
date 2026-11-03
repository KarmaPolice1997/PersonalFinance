using PersonalFinance.Models;
using PersonalFinance.Services;

namespace PersonalFinance.UI.Dialogs;

public class TransactionDialog : Form
{
    private readonly IFinanceService _financeService;
    private readonly Transaction? _editingTransaction;

    private ComboBox _cboType = null!;
    private DateTimePicker _dtpDate = null!;
    private NumericUpDown _numAmount = null!;
    private ComboBox _cboCategory = null!;
    private ComboBox _cboSourceAccount = null!;
    private ComboBox _cboDestAccount = null!;
    private Label _lblDestAccount = null!;
    private TextBox _txtNote = null!;
    private Button _btnSave = null!;
    private Button _btnCancel = null!;

    public Transaction? ResultTransaction { get; private set; }

    public TransactionDialog(IFinanceService financeService, Transaction? transactionToEdit = null)
    {
        _financeService = financeService;
        _editingTransaction = transactionToEdit;

        InitializeComponent();
        LoadData();

        if (_editingTransaction != null)
        {
            Text = "Cap nhat giao dich";
            PopulateData(_editingTransaction);
        }
        else
        {
            Text = "Them giao dich moi";
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
        ClientSize = new Size(460, 480);

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Theme.GridHeaderBg,
            Padding = new Padding(20, 0, 0, 0)
        };
        var lblTitle = new Label
        {
            Text = _editingTransaction != null ? "Chinh sua thong tin giao dich" : "Ghi nhan giao dich moi",
            Font = Theme.FontSectionTitle,
            ForeColor = Theme.TextPrimary,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        pnlHeader.Controls.Add(lblTitle);

        // Body table layout
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 7,
            Padding = new Padding(24, 20, 24, 10),
            AutoSize = false
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        for (int i = 0; i < 7; i++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
        }

        // 1. Loai giao dich
        var lblType = new Label { Text = "Loai giao dich:", Anchor = AnchorStyles.Left, AutoSize = true, Font = Theme.FontBodyBold };
        _cboType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Font = Theme.FontBody };
        _cboType.Items.Add(new ComboBoxItem("Chi tieu", TransactionType.Expense));
        _cboType.Items.Add(new ComboBoxItem("Thu nhap", TransactionType.Income));
        _cboType.Items.Add(new ComboBoxItem("Chuyen khoan", TransactionType.Transfer));
        _cboType.SelectedIndex = 0;
        _cboType.SelectedIndexChanged += OnTypeChanged;

        // 2. Ngay giao dich
        var lblDate = new Label { Text = "Ngay giao dich:", Anchor = AnchorStyles.Left, AutoSize = true, Font = Theme.FontBodyBold };
        _dtpDate = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            Dock = DockStyle.Fill,
            Font = Theme.FontBody
        };

        // 3. So tien
        var lblAmount = new Label { Text = "So tien (VND):", Anchor = AnchorStyles.Left, AutoSize = true, Font = Theme.FontBodyBold };
        _numAmount = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            Font = Theme.FontBody,
            Minimum = 1000,
            Maximum = 1000000000000M,
            Increment = 50000,
            ThousandsSeparator = true
        };

        // 4. Danh muc
        var lblCategory = new Label { Text = "Danh muc:", Anchor = AnchorStyles.Left, AutoSize = true, Font = Theme.FontBodyBold };
        _cboCategory = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Font = Theme.FontBody };

        // 5. Tai khoan nguon / Vi
        var lblSourceAccount = new Label { Text = "Tai khoan / Vi:", Anchor = AnchorStyles.Left, AutoSize = true, Font = Theme.FontBodyBold };
        _cboSourceAccount = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Font = Theme.FontBody };

        // 6. Tai khoan dich (cho chuyen khoan)
        _lblDestAccount = new Label { Text = "Vi nhan tien:", Anchor = AnchorStyles.Left, AutoSize = true, Font = Theme.FontBodyBold, Visible = false };
        _cboDestAccount = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Font = Theme.FontBody, Visible = false };

        // 7. Ghi chu
        var lblNote = new Label { Text = "Ghi chu:", Anchor = AnchorStyles.Left, AutoSize = true, Font = Theme.FontBodyBold };
        _txtNote = new TextBox { Dock = DockStyle.Fill, Font = Theme.FontBody };

        // Add to layout
        layout.Controls.Add(lblType, 0, 0);
        layout.Controls.Add(_cboType, 1, 0);
        layout.Controls.Add(lblDate, 0, 1);
        layout.Controls.Add(_dtpDate, 1, 1);
        layout.Controls.Add(lblAmount, 0, 2);
        layout.Controls.Add(_numAmount, 1, 2);
        layout.Controls.Add(lblCategory, 0, 3);
        layout.Controls.Add(_cboCategory, 1, 3);
        layout.Controls.Add(lblSourceAccount, 0, 4);
        layout.Controls.Add(_cboSourceAccount, 1, 4);
        layout.Controls.Add(_lblDestAccount, 0, 5);
        layout.Controls.Add(_cboDestAccount, 1, 5);
        layout.Controls.Add(lblNote, 0, 6);
        layout.Controls.Add(_txtNote, 1, 6);

        // Buttons Footer
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
            Location = new Point(230, 11)
        };
        Theme.ApplySecondaryButton(_btnCancel);
        _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        _btnSave = new Button
        {
            Text = "Luu lai",
            Size = new Size(100, 38),
            Anchor = AnchorStyles.Right,
            Location = new Point(340, 11)
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

    private void LoadData()
    {
        // Load accounts
        var accounts = _financeService.GetAllAccounts();
        _cboSourceAccount.Items.Clear();
        _cboDestAccount.Items.Clear();

        foreach (var acc in accounts)
        {
            _cboSourceAccount.Items.Add(new ComboBoxItem(acc.Name, acc.Id));
            _cboDestAccount.Items.Add(new ComboBoxItem(acc.Name, acc.Id));
        }

        if (_cboSourceAccount.Items.Count > 0)
            _cboSourceAccount.SelectedIndex = 0;

        if (_cboDestAccount.Items.Count > 1)
            _cboDestAccount.SelectedIndex = 1;
        else if (_cboDestAccount.Items.Count > 0)
            _cboDestAccount.SelectedIndex = 0;

        ReloadCategories();
    }

    private void ReloadCategories()
    {
        var currentType = GetSelectedType();
        _cboCategory.Items.Clear();

        if (currentType == TransactionType.Transfer)
        {
            _cboCategory.Enabled = false;
            return;
        }

        _cboCategory.Enabled = true;
        var categories = _financeService.GetAllCategories(currentType);
        foreach (var cat in categories)
        {
            _cboCategory.Items.Add(new ComboBoxItem(cat.Name, cat.Id));
        }

        if (_cboCategory.Items.Count > 0)
            _cboCategory.SelectedIndex = 0;
    }

    private void OnTypeChanged(object? sender, EventArgs e)
    {
        var type = GetSelectedType();
        bool isTransfer = type == TransactionType.Transfer;

        _lblDestAccount.Visible = isTransfer;
        _cboDestAccount.Visible = isTransfer;
        ReloadCategories();
    }

    private TransactionType GetSelectedType()
    {
        if (_cboType.SelectedItem is ComboBoxItem item && item.Value is TransactionType type)
            return type;
        return TransactionType.Expense;
    }

    private void PopulateData(Transaction tx)
    {
        for (int i = 0; i < _cboType.Items.Count; i++)
        {
            if (_cboType.Items[i] is ComboBoxItem item && (TransactionType)item.Value == tx.Type)
            {
                _cboType.SelectedIndex = i;
                break;
            }
        }

        _dtpDate.Value = tx.TransactionDate;
        _numAmount.Value = Math.Max(_numAmount.Minimum, tx.Amount);
        _txtNote.Text = tx.Note;

        // Select Source Account
        for (int i = 0; i < _cboSourceAccount.Items.Count; i++)
        {
            if (_cboSourceAccount.Items[i] is ComboBoxItem item && (string)item.Value == tx.SourceAccountId)
            {
                _cboSourceAccount.SelectedIndex = i;
                break;
            }
        }

        // Select Dest Account
        for (int i = 0; i < _cboDestAccount.Items.Count; i++)
        {
            if (_cboDestAccount.Items[i] is ComboBoxItem item && (string)item.Value == tx.DestinationAccountId)
            {
                _cboDestAccount.SelectedIndex = i;
                break;
            }
        }

        // Select Category
        for (int i = 0; i < _cboCategory.Items.Count; i++)
        {
            if (_cboCategory.Items[i] is ComboBoxItem item && (string)item.Value == tx.CategoryId)
            {
                _cboCategory.SelectedIndex = i;
                break;
            }
        }
    }

    private void OnSaveClicked(object? sender, EventArgs e)
    {
        var type = GetSelectedType();
        if (_numAmount.Value <= 0)
        {
            MessageBox.Show("So tien phai lon hon 0.", "Loi nhap lieu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_cboSourceAccount.SelectedItem is not ComboBoxItem sourceAccItem)
        {
            MessageBox.Show("Vui long chon tai khoan thanh toan.", "Loi nhap lieu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string sourceAccId = (string)sourceAccItem.Value;
        string? destAccId = null;
        string? categoryId = null;

        if (type == TransactionType.Transfer)
        {
            if (_cboDestAccount.SelectedItem is not ComboBoxItem destAccItem)
            {
                MessageBox.Show("Vui long chon tai khoan nhan tien.", "Loi nhap lieu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            destAccId = (string)destAccItem.Value;

            if (sourceAccId == destAccId)
            {
                MessageBox.Show("Tai khoan nguon va tai khoan nhan phai khac nhau.", "Loi nhap lieu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }
        else
        {
            if (_cboCategory.SelectedItem is ComboBoxItem catItem)
            {
                categoryId = (string)catItem.Value;
            }
        }

        ResultTransaction = new Transaction
        {
            Id = _editingTransaction?.Id ?? Guid.NewGuid().ToString("N"),
            TransactionDate = _dtpDate.Value.Date,
            Amount = _numAmount.Value,
            Type = type,
            CategoryId = categoryId,
            SourceAccountId = sourceAccId,
            DestinationAccountId = destAccId,
            Note = _txtNote.Text.Trim(),
            CreatedAt = _editingTransaction?.CreatedAt ?? DateTime.Now
        };

        DialogResult = DialogResult.OK;
        Close();
    }
}

public class ComboBoxItem
{
    public string Text { get; }
    public object Value { get; }

    public ComboBoxItem(string text, object value)
    {
        Text = text;
        Value = value;
    }

    public override string ToString() => Text;
}
