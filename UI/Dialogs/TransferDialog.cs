using PersonalFinance.Services;

namespace PersonalFinance.UI.Dialogs;

public class TransferDialog : Form
{
    private readonly IFinanceService _financeService;

    private ComboBox _cboFrom = null!;
    private ComboBox _cboTo = null!;
    private NumericUpDown _numAmount = null!;
    private DateTimePicker _dtpDate = null!;
    private TextBox _txtNote = null!;
    private Button _btnTransfer = null!;
    private Button _btnCancel = null!;

    public TransferDialog(IFinanceService financeService)
    {
        _financeService = financeService;
        InitializeComponent();
        LoadAccounts();
    }

    private void InitializeComponent()
    {
        Text = "Chuyển tiền giữa các ví / tài khoản";
        Font = Theme.FontApp;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(450, 280);

        var grp = new GroupBox
        {
            Text = "Thông tin chuyển khoản nội bộ",
            Dock = DockStyle.Top,
            Height = 220,
            Location = new Point(12, 10),
            Padding = new Padding(12)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(8)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        for (int i = 0; i < 5; i++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        }

        var lblFrom = new Label { Text = "Từ ví / tài khoản:", Anchor = AnchorStyles.Left, AutoSize = true };
        _cboFrom = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };

        var lblTo = new Label { Text = "Đến ví / tài khoản:", Anchor = AnchorStyles.Left, AutoSize = true };
        _cboTo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };

        var lblAmount = new Label { Text = "Số tiền chuyển (VND):", Anchor = AnchorStyles.Left, AutoSize = true };
        _numAmount = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            Minimum = 1000,
            Maximum = 1000000000000M,
            Increment = 100000,
            ThousandsSeparator = true
        };

        var lblDate = new Label { Text = "Ngày chuyển:", Anchor = AnchorStyles.Left, AutoSize = true };
        _dtpDate = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            Dock = DockStyle.Fill
        };

        var lblNote = new Label { Text = "Ghi chú:", Anchor = AnchorStyles.Left, AutoSize = true };
        _txtNote = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Ví dụ: Rút tiền ATM, Nạp ví điện tử..." };

        layout.Controls.Add(lblFrom, 0, 0);
        layout.Controls.Add(_cboFrom, 1, 0);
        layout.Controls.Add(lblTo, 0, 1);
        layout.Controls.Add(_cboTo, 1, 1);
        layout.Controls.Add(lblAmount, 0, 2);
        layout.Controls.Add(_numAmount, 1, 2);
        layout.Controls.Add(lblDate, 0, 3);
        layout.Controls.Add(_dtpDate, 1, 3);
        layout.Controls.Add(lblNote, 0, 4);
        layout.Controls.Add(_txtNote, 1, 4);

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

        _btnTransfer = new Button
        {
            Text = "Xác nhận",
            Size = new Size(85, 30),
            Anchor = AnchorStyles.Right,
            Location = new Point(348, 6)
        };
        _btnTransfer.Click += OnTransferClicked;

        pnlButtons.Controls.Add(_btnCancel);
        pnlButtons.Controls.Add(_btnTransfer);

        Controls.Add(grp);
        Controls.Add(pnlButtons);

        AcceptButton = _btnTransfer;
        CancelButton = _btnCancel;
    }

    private void LoadAccounts()
    {
        var accounts = _financeService.GetAllAccounts();
        _cboFrom.Items.Clear();
        _cboTo.Items.Clear();

        foreach (var acc in accounts)
        {
            _cboFrom.Items.Add(new ComboBoxItem(acc.Name, acc.Id));
            _cboTo.Items.Add(new ComboBoxItem(acc.Name, acc.Id));
        }

        if (_cboFrom.Items.Count > 0)
            _cboFrom.SelectedIndex = 0;

        if (_cboTo.Items.Count > 1)
            _cboTo.SelectedIndex = 1;
        else if (_cboTo.Items.Count > 0)
            _cboTo.SelectedIndex = 0;
    }

    private void OnTransferClicked(object? sender, EventArgs e)
    {
        if (_cboFrom.SelectedItem is not ComboBoxItem fromItem || _cboTo.SelectedItem is not ComboBoxItem toItem)
        {
            MessageBox.Show("Vui lòng chọn đầy đủ ví gửi và ví nhận.", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string fromId = (string)fromItem.Value;
        string toId = (string)toItem.Value;

        if (fromId == toId)
        {
            MessageBox.Show("Ví gửi và ví nhận tiền phải khác nhau.", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_numAmount.Value <= 0)
        {
            MessageBox.Show("Số tiền chuyển phải lớn hơn 0.", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            _financeService.TransferFunds(fromId, toId, _numAmount.Value, _dtpDate.Value.Date, _txtNote.Text.Trim());
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi chuyển tiền: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
