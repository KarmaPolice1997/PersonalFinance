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
            Text = "Cập nhật giao dịch";
            PopulateData(_editingTransaction);
        }
        else
        {
            Text = "Ghi nhận giao dịch mới";
        }
    }

    private void InitializeComponent()
    {
        Font = Theme.FontApp;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(450, 360);

        var grp = new GroupBox
        {
            Text = "Thông tin giao dịch",
            Dock = DockStyle.Top,
            Height = 290,
            Location = new Point(12, 10),
            Padding = new Padding(12)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 7,
            Padding = new Padding(8)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        for (int i = 0; i < 7; i++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        }

        var lblType = new Label { Text = "Loại giao dịch:", Anchor = AnchorStyles.Left, AutoSize = true };
        _cboType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
        _cboType.Items.Add(new ComboBoxItem("Chi tiêu", TransactionType.Expense));
        _cboType.Items.Add(new ComboBoxItem("Thu nhập", TransactionType.Income));
        _cboType.Items.Add(new ComboBoxItem("Chuyển tiền nội bộ", TransactionType.Transfer));
        _cboType.SelectedIndex = 0;
        _cboType.SelectedIndexChanged += OnTypeChanged;

        var lblDate = new Label { Text = "Ngày giao dịch:", Anchor = AnchorStyles.Left, AutoSize = true };
        _dtpDate = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            Dock = DockStyle.Fill
        };

        var lblAmount = new Label { Text = "Số tiền (VND):", Anchor = AnchorStyles.Left, AutoSize = true };
        _numAmount = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            Minimum = 1000,
            Maximum = 1000000000000M,
            Increment = 50000,
            ThousandsSeparator = true
        };

        var lblCategory = new Label { Text = "Danh mục:", Anchor = AnchorStyles.Left, AutoSize = true };
        _cboCategory = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };

        var lblSourceAccount = new Label { Text = "Ví / Tài khoản:", Anchor = AnchorStyles.Left, AutoSize = true };
        _cboSourceAccount = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };

        _lblDestAccount = new Label { Text = "Ví nhận tiền:", Anchor = AnchorStyles.Left, AutoSize = true, Visible = false };
        _cboDestAccount = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Visible = false };

        var lblNote = new Label { Text = "Ghi chú:", Anchor = AnchorStyles.Left, AutoSize = true };
        _txtNote = new TextBox { Dock = DockStyle.Fill };

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
            Location = new Point(255, 6)
        };
        _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        _btnSave = new Button
        {
            Text = "Lưu lại",
            Size = new Size(85, 30),
            Anchor = AnchorStyles.Right,
            Location = new Point(348, 6)
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

    private void LoadData()
    {
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

        for (int i = 0; i < _cboSourceAccount.Items.Count; i++)
        {
            if (_cboSourceAccount.Items[i] is ComboBoxItem item && (string)item.Value == tx.SourceAccountId)
            {
                _cboSourceAccount.SelectedIndex = i;
                break;
            }
        }

        for (int i = 0; i < _cboDestAccount.Items.Count; i++)
        {
            if (_cboDestAccount.Items[i] is ComboBoxItem item && (string)item.Value == tx.DestinationAccountId)
            {
                _cboDestAccount.SelectedIndex = i;
                break;
            }
        }

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
            MessageBox.Show("Số tiền phải lớn hơn 0.", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_cboSourceAccount.SelectedItem is not ComboBoxItem sourceAccItem)
        {
            MessageBox.Show("Vui lòng chọn tài khoản/ví thanh toán.", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string sourceAccId = (string)sourceAccItem.Value;
        string? destAccId = null;
        string? categoryId = null;

        if (type == TransactionType.Transfer)
        {
            if (_cboDestAccount.SelectedItem is not ComboBoxItem destAccItem)
            {
                MessageBox.Show("Vui lòng chọn ví nhận tiền.", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            destAccId = (string)destAccItem.Value;

            if (sourceAccId == destAccId)
            {
                MessageBox.Show("Ví gửi và ví nhận phải khác nhau.", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
