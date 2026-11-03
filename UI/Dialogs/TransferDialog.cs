using PersonalFinance.Models;
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
        Text = "Chuyen tien giua cac vi / tai khoan";
        Font = Theme.FontBody;
        BackColor = Theme.BgCard;
        ForeColor = Theme.TextPrimary;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(460, 390);

        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Theme.GridHeaderBg,
            Padding = new Padding(20, 0, 0, 0)
        };
        var lblTitle = new Label
        {
            Text = "Chuyen tien noi bo giua cac vi",
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
            RowCount = 5,
            Padding = new Padding(24, 20, 24, 10)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        for (int i = 0; i < 5; i++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
        }

        var lblFrom = new Label { Text = "Tu vi / tai khoan:", Anchor = AnchorStyles.Left, AutoSize = true, Font = Theme.FontBodyBold };
        _cboFrom = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Font = Theme.FontBody };

        var lblTo = new Label { Text = "Den vi / tai khoan:", Anchor = AnchorStyles.Left, AutoSize = true, Font = Theme.FontBodyBold };
        _cboTo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Font = Theme.FontBody };

        var lblAmount = new Label { Text = "So tien (VND):", Anchor = AnchorStyles.Left, AutoSize = true, Font = Theme.FontBodyBold };
        _numAmount = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            Font = Theme.FontBody,
            Minimum = 1000,
            Maximum = 1000000000000M,
            Increment = 100000,
            ThousandsSeparator = true
        };

        var lblDate = new Label { Text = "Ngay chuyen:", Anchor = AnchorStyles.Left, AutoSize = true, Font = Theme.FontBodyBold };
        _dtpDate = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            Dock = DockStyle.Fill,
            Font = Theme.FontBody
        };

        var lblNote = new Label { Text = "Ghi chu:", Anchor = AnchorStyles.Left, AutoSize = true, Font = Theme.FontBodyBold };
        _txtNote = new TextBox { Dock = DockStyle.Fill, Font = Theme.FontBody, PlaceholderText = "Vi du: Nap tien vao vi MoMo, rut ATM..." };

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

        _btnTransfer = new Button
        {
            Text = "Xac nhan",
            Size = new Size(100, 38),
            Anchor = AnchorStyles.Right,
            Location = new Point(340, 11)
        };
        Theme.ApplyPrimaryButton(_btnTransfer);
        _btnTransfer.Click += OnTransferClicked;

        pnlFooter.Controls.Add(_btnCancel);
        pnlFooter.Controls.Add(_btnTransfer);

        Controls.Add(layout);
        Controls.Add(pnlFooter);
        Controls.Add(pnlHeader);

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
            MessageBox.Show("Vui long chon day du tai khoan nguon va tai khoan dich.", "Loi nhap lieu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var fromId = (string)fromItem.Value;
        var toId = (string)toItem.Value;

        if (fromId == toId)
        {
            MessageBox.Show("Tai khoan chuyen va tai khoan nhan phai khac nhau.", "Loi nhap lieu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_numAmount.Value <= 0)
        {
            MessageBox.Show("So tien chuyen phai lon hon 0.", "Loi nhap lieu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            MessageBox.Show($"Loi khi chuyen tien: {ex.Message}", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
