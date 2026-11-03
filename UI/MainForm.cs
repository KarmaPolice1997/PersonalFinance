using PersonalFinance.Models;
using PersonalFinance.Services;
using PersonalFinance.UI.Dialogs;

namespace PersonalFinance.UI;

public partial class MainForm : Form
{
    private readonly IFinanceService _financeService;

    // Controls chính
    private TabControl _tabControl = null!;
    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _statusDb = null!;
    private ToolStripStatusLabel _statusBalance = null!;

    // Tab 1: Tổng quan
    private Label _lblAssetValue = null!;
    private Label _lblIncomeValue = null!;
    private Label _lblExpenseValue = null!;
    private Label _lblSavingsValue = null!;
    private DataGridView _gridRecent = null!;

    // Tab 2: Sổ giao dịch
    private DataGridView _gridTransactions = null!;
    private ComboBox _cboTxType = null!;
    private ComboBox _cboTxAccount = null!;
    private DateTimePicker _dtpTxFrom = null!;
    private DateTimePicker _dtpTxTo = null!;
    private TextBox _txtTxSearch = null!;
    private Label _lblTxSummary = null!;

    // Tab 3: Tài khoản & Ví
    private DataGridView _gridAccounts = null!;
    private Label _lblAccTotal = null!;

    // Tab 4: Danh mục
    private DataGridView _gridCategories = null!;
    private ComboBox _cboCatFilter = null!;

    // Tab 5: Báo cáo
    private DateTimePicker _dtpRepFrom = null!;
    private DateTimePicker _dtpRepTo = null!;
    private Label _lblRepIncome = null!;
    private Label _lblRepExpense = null!;
    private Label _lblRepSavings = null!;
    private Label _lblRepRate = null!;
    private DataGridView _gridRepExpenses = null!;
    private DataGridView _gridRepAccounts = null!;

    // Tab 6: Sao lưu & Khôi phục
    private TextBox _txtDbPath = null!;
    private Label _lblDbSize = null!;

    public MainForm(IFinanceService financeService)
    {
        _financeService = financeService;
        InitializeComponent();

        _financeService.DataChanged += (s, e) =>
        {
            if (IsHandleCreated && !IsDisposed)
            {
                BeginInvoke(new Action(RefreshAllData));
            }
        };

        RefreshAllData();
    }

    private void InitializeComponent()
    {
        Text = "Quản Lý Tài Chính Cá Nhân";
        Font = Theme.FontApp;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1000, 650);
        Size = new Size(1100, 700);

        // 1. Menu Bar chuẩn Windows
        var menuStrip = new MenuStrip { Font = Theme.FontApp };
        var mnuFile = new ToolStripMenuItem("Tệp tin");
        mnuFile.DropDownItems.Add("Sao lưu cơ sở dữ liệu...", null, (s, e) => PerformBackup());
        mnuFile.DropDownItems.Add("Phục hồi cơ sở dữ liệu...", null, (s, e) => PerformRestore());
        mnuFile.DropDownItems.Add(new ToolStripSeparator());
        mnuFile.DropDownItems.Add("Thoát", null, (s, e) => Close());

        var mnuAction = new ToolStripMenuItem("Thao tác");
        mnuAction.DropDownItems.Add("Ghi nhận giao dịch...", null, (s, e) => OpenAddTransaction(TransactionType.Expense));
        mnuAction.DropDownItems.Add("Chuyển tiền nội bộ...", null, (s, e) => OpenTransferDialog());
        mnuAction.DropDownItems.Add("Thêm ví / tài khoản...", null, (s, e) => OpenAddAccount());
        mnuAction.DropDownItems.Add("Thêm danh mục...", null, (s, e) => OpenAddCategory());

        var mnuHelp = new ToolStripMenuItem("Trợ giúp");
        mnuHelp.DropDownItems.Add("Thông tin ứng dụng", null, (s, e) =>
        {
            MessageBox.Show(
                "Phần mềm Quản Lý Tài Chính Cá Nhân\n" +
                "Nền tảng: WinForms (.NET 10 / C#)\n" +
                "Cơ sở dữ liệu: SQLite (winsqlite3 native engine)\n" +
                "Kiến trúc: Chuẩn hướng đối tượng UML\n" +
                "Hỗ trợ đầy đủ Tiếng Việt và Sao lưu/Phục hồi dữ liệu.",
                "Giới thiệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
        });

        menuStrip.Items.Add(mnuFile);
        menuStrip.Items.Add(mnuAction);
        menuStrip.Items.Add(mnuHelp);
        MainMenuStrip = menuStrip;
        Controls.Add(menuStrip);

        // 2. Status Bar ở đáy
        _statusStrip = new StatusStrip { Font = Theme.FontApp };
        _statusDb = new ToolStripStatusLabel { Text = $"CSDL: {_financeService.DatabasePath}" };
        _statusBalance = new ToolStripStatusLabel { Text = "Tổng tài sản: 0 VND", Alignment = ToolStripItemAlignment.Right };
        _statusStrip.Items.Add(_statusDb);
        _statusStrip.Items.Add(new ToolStripStatusLabel { Spring = true });
        _statusStrip.Items.Add(_statusBalance);
        Controls.Add(_statusStrip);

        // 3. TabControl chính
        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = Theme.FontApp,
            Padding = new Point(12, 6)
        };

        _tabControl.TabPages.Add(CreateOverviewTab());
        _tabControl.TabPages.Add(CreateTransactionsTab());
        _tabControl.TabPages.Add(CreateAccountsTab());
        _tabControl.TabPages.Add(CreateCategoriesTab());
        _tabControl.TabPages.Add(CreateReportsTab());
        _tabControl.TabPages.Add(CreateBackupTab());

        _tabControl.SelectedIndexChanged += (s, e) => RefreshCurrentTab();

        Controls.Add(_tabControl);
        _tabControl.BringToFront();
    }

    #region Tab 1: Tổng quan
    private TabPage CreateOverviewTab()
    {
        var tab = new TabPage("Tổng quan") { Padding = new Padding(12) };

        // GroupBox: Các chỉ số tài chính
        var grpStats = new GroupBox
        {
            Text = "Thông số tài chính hiện tại",
            Dock = DockStyle.Top,
            Height = 100,
            Padding = new Padding(10)
        };

        var layoutStats = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1
        };
        for (int i = 0; i < 4; i++) layoutStats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

        layoutStats.Controls.Add(CreateStatBox("TỔNG TÀI SẢN KHẢ DỤNG", out _lblAssetValue, SystemColors.ControlText), 0, 0);
        layoutStats.Controls.Add(CreateStatBox("TỔNG THU NHẬP", out _lblIncomeValue, Theme.ColorIncome), 1, 0);
        layoutStats.Controls.Add(CreateStatBox("TỔNG CHI TIÊU", out _lblExpenseValue, Theme.ColorExpense), 2, 0);
        layoutStats.Controls.Add(CreateStatBox("TIẾT KIỆM THUẦN", out _lblSavingsValue, SystemColors.HotTrack), 3, 0);

        grpStats.Controls.Add(layoutStats);
        tab.Controls.Add(grpStats);

        // Panel: Nút thao tác nhanh
        var pnlQuick = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(0, 8, 0, 4)
        };

        var btnAddExpense = new Button { Text = "+ Ghi nhận chi tiêu", Width = 150, Height = 30, Location = new Point(0, 6) };
        btnAddExpense.Click += (s, e) => OpenAddTransaction(TransactionType.Expense);

        var btnAddIncome = new Button { Text = "+ Ghi nhận thu nhập", Width = 150, Height = 30, Location = new Point(160, 6) };
        btnAddIncome.Click += (s, e) => OpenAddTransaction(TransactionType.Income);

        var btnTransfer = new Button { Text = "-> Chuyển khoản nội bộ", Width = 160, Height = 30, Location = new Point(320, 6) };
        btnTransfer.Click += (s, e) => OpenTransferDialog();

        var btnRefresh = new Button { Text = "Làm mới", Width = 90, Height = 30, Location = new Point(490, 6) };
        btnRefresh.Click += (s, e) => RefreshAllData();

        pnlQuick.Controls.Add(btnAddExpense);
        pnlQuick.Controls.Add(btnAddIncome);
        pnlQuick.Controls.Add(btnTransfer);
        pnlQuick.Controls.Add(btnRefresh);

        tab.Controls.Add(pnlQuick);

        // GroupBox: Giao dịch gần đây
        var grpRecent = new GroupBox
        {
            Text = "Giao dịch gần đây nhất",
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        _gridRecent = new DataGridView { Dock = DockStyle.Fill };
        Theme.FormatGrid(_gridRecent);
        _gridRecent.Columns.Add("Date", "Ngày");
        _gridRecent.Columns.Add("Type", "Loại");
        _gridRecent.Columns.Add("Category", "Danh mục");
        _gridRecent.Columns.Add("Account", "Tài khoản / Ví");
        _gridRecent.Columns.Add("Amount", "Số tiền (VND)");
        _gridRecent.Columns.Add("Note", "Ghi chú");

        if (_gridRecent.Columns["Date"] != null) _gridRecent.Columns["Date"]!.Width = 100;
        if (_gridRecent.Columns["Type"] != null) _gridRecent.Columns["Type"]!.Width = 110;
        if (_gridRecent.Columns["Category"] != null) _gridRecent.Columns["Category"]!.Width = 150;
        if (_gridRecent.Columns["Account"] != null) _gridRecent.Columns["Account"]!.Width = 180;
        if (_gridRecent.Columns["Amount"] != null)
        {
            _gridRecent.Columns["Amount"]!.Width = 140;
            _gridRecent.Columns["Amount"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        grpRecent.Controls.Add(_gridRecent);
        tab.Controls.Add(grpRecent);

        grpRecent.BringToFront();
        return tab;
    }

    private Control CreateStatBox(string title, out Label valLabel, Color valColor)
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4) };
        var lblTitle = new Label { Text = title, Dock = DockStyle.Top, Height = 18, Font = Theme.FontApp, ForeColor = SystemColors.GrayText };
        valLabel = new Label { Text = "0 VND", Dock = DockStyle.Fill, Font = Theme.FontStat, ForeColor = valColor, TextAlign = ContentAlignment.MiddleLeft };
        panel.Controls.Add(valLabel);
        panel.Controls.Add(lblTitle);
        return panel;
    }
    #endregion

    #region Tab 2: Sổ giao dịch
    private TabPage CreateTransactionsTab()
    {
        var tab = new TabPage("Sổ giao dịch") { Padding = new Padding(12) };

        // GroupBox Bộ lọc
        var grpFilter = new GroupBox
        {
            Text = "Bộ lọc và tìm kiếm giao dịch",
            Dock = DockStyle.Top,
            Height = 85,
            Padding = new Padding(10)
        };

        var pnlFilterControls = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = false,
            WrapContents = false
        };

        pnlFilterControls.Controls.Add(new Label { Text = "Loại:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
        _cboTxType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
        _cboTxType.Items.Add(new ComboBoxItem("Tất cả", null!));
        _cboTxType.Items.Add(new ComboBoxItem("Chi tiêu", TransactionType.Expense));
        _cboTxType.Items.Add(new ComboBoxItem("Thu nhập", TransactionType.Income));
        _cboTxType.Items.Add(new ComboBoxItem("Chuyển tiền", TransactionType.Transfer));
        _cboTxType.SelectedIndex = 0;
        _cboTxType.SelectedIndexChanged += (s, e) => LoadTransactions();
        pnlFilterControls.Controls.Add(_cboTxType);

        pnlFilterControls.Controls.Add(new Label { Text = "  Ví:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
        _cboTxAccount = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
        _cboTxAccount.SelectedIndexChanged += (s, e) => LoadTransactions();
        pnlFilterControls.Controls.Add(_cboTxAccount);

        pnlFilterControls.Controls.Add(new Label { Text = "  Từ ngày:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
        _dtpTxFrom = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Width = 100, Value = DateTime.Today.AddMonths(-1) };
        _dtpTxFrom.ValueChanged += (s, e) => LoadTransactions();
        pnlFilterControls.Controls.Add(_dtpTxFrom);

        pnlFilterControls.Controls.Add(new Label { Text = "  Đến ngày:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
        _dtpTxTo = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Width = 100, Value = DateTime.Today };
        _dtpTxTo.ValueChanged += (s, e) => LoadTransactions();
        pnlFilterControls.Controls.Add(_dtpTxTo);

        pnlFilterControls.Controls.Add(new Label { Text = "  Tìm:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
        _txtTxSearch = new TextBox { Width = 130, PlaceholderText = "Ghi chú..." };
        _txtTxSearch.TextChanged += (s, e) => LoadTransactions();
        pnlFilterControls.Controls.Add(_txtTxSearch);

        var btnResetFilter = new Button { Text = "Đặt lại", Width = 70, Height = 26 };
        btnResetFilter.Click += (s, e) =>
        {
            _cboTxType.SelectedIndex = 0;
            _cboTxAccount.SelectedIndex = 0;
            _dtpTxFrom.Value = DateTime.Today.AddMonths(-1);
            _dtpTxTo.Value = DateTime.Today;
            _txtTxSearch.Text = "";
            LoadTransactions();
        };
        pnlFilterControls.Controls.Add(btnResetFilter);

        grpFilter.Controls.Add(pnlFilterControls);
        tab.Controls.Add(grpFilter);

        // Thanh công cụ Action
        var pnlActions = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(0, 6, 0, 4) };
        var btnAdd = new Button { Text = "+ Thêm giao dịch", Width = 130, Height = 28, Location = new Point(0, 5) };
        btnAdd.Click += (s, e) => OpenAddTransaction();

        var btnEdit = new Button { Text = "Chỉnh sửa", Width = 90, Height = 28, Location = new Point(135, 5) };
        btnEdit.Click += OnEditTransactionClicked;

        var btnDelete = new Button { Text = "Xóa", Width = 80, Height = 28, Location = new Point(230, 5) };
        btnDelete.Click += OnDeleteTransactionClicked;

        pnlActions.Controls.Add(btnAdd);
        pnlActions.Controls.Add(btnEdit);
        pnlActions.Controls.Add(btnDelete);
        tab.Controls.Add(pnlActions);

        // Grid
        var grpGrid = new GroupBox { Text = "Danh sách chi tiết các giao dịch", Dock = DockStyle.Fill, Padding = new Padding(10) };
        _gridTransactions = new DataGridView { Dock = DockStyle.Fill };
        Theme.FormatGrid(_gridTransactions);
        _gridTransactions.Columns.Add("Id", "Mã");
        if (_gridTransactions.Columns["Id"] != null) _gridTransactions.Columns["Id"]!.Visible = false;

        _gridTransactions.Columns.Add("Date", "Ngày");
        _gridTransactions.Columns.Add("Type", "Phân loại");
        _gridTransactions.Columns.Add("Category", "Danh mục");
        _gridTransactions.Columns.Add("SourceAccount", "Ví thanh toán / nguồn");
        _gridTransactions.Columns.Add("DestAccount", "Ví nhận (nếu có)");
        _gridTransactions.Columns.Add("Note", "Ghi chú");
        _gridTransactions.Columns.Add("Amount", "Số tiền (VND)");

        if (_gridTransactions.Columns["Date"] != null) _gridTransactions.Columns["Date"]!.Width = 95;
        if (_gridTransactions.Columns["Type"] != null) _gridTransactions.Columns["Type"]!.Width = 105;
        if (_gridTransactions.Columns["Category"] != null) _gridTransactions.Columns["Category"]!.Width = 140;
        if (_gridTransactions.Columns["SourceAccount"] != null) _gridTransactions.Columns["SourceAccount"]!.Width = 150;
        if (_gridTransactions.Columns["DestAccount"] != null) _gridTransactions.Columns["DestAccount"]!.Width = 150;
        if (_gridTransactions.Columns["Amount"] != null)
        {
            _gridTransactions.Columns["Amount"]!.Width = 140;
            _gridTransactions.Columns["Amount"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        _gridTransactions.CellDoubleClick += (s, e) => OnEditTransactionClicked(s, e);

        _lblTxSummary = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 26,
            Font = Theme.FontAppBold,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "Tổng số giao dịch: 0"
        };

        grpGrid.Controls.Add(_gridTransactions);
        grpGrid.Controls.Add(_lblTxSummary);
        tab.Controls.Add(grpGrid);

        grpGrid.BringToFront();
        return tab;
    }
    #endregion

    #region Tab 3: Tài khoản & Ví
    private TabPage CreateAccountsTab()
    {
        var tab = new TabPage("Tài khoản & Ví") { Padding = new Padding(12) };

        var pnlActions = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(0, 4, 0, 6) };
        var btnAdd = new Button { Text = "+ Thêm ví / tài khoản", Width = 150, Height = 28, Location = new Point(0, 4) };
        btnAdd.Click += (s, e) => OpenAddAccount();

        var btnTransfer = new Button { Text = "-> Chuyển tiền giữa các ví", Width = 170, Height = 28, Location = new Point(155, 4) };
        btnTransfer.Click += (s, e) => OpenTransferDialog();

        var btnEdit = new Button { Text = "Sửa", Width = 70, Height = 28, Location = new Point(330, 4) };
        btnEdit.Click += OnEditAccountClicked;

        var btnDelete = new Button { Text = "Xóa", Width = 70, Height = 28, Location = new Point(405, 4) };
        btnDelete.Click += OnDeleteAccountClicked;

        pnlActions.Controls.Add(btnAdd);
        pnlActions.Controls.Add(btnTransfer);
        pnlActions.Controls.Add(btnEdit);
        pnlActions.Controls.Add(btnDelete);
        tab.Controls.Add(pnlActions);

        var grpGrid = new GroupBox { Text = "Danh sách ví tiền và tài khoản thanh toán", Dock = DockStyle.Fill, Padding = new Padding(10) };
        _gridAccounts = new DataGridView { Dock = DockStyle.Fill };
        Theme.FormatGrid(_gridAccounts);
        _gridAccounts.Columns.Add("Id", "Mã");
        if (_gridAccounts.Columns["Id"] != null) _gridAccounts.Columns["Id"]!.Visible = false;

        _gridAccounts.Columns.Add("Name", "Tên ví / Tài khoản");
        _gridAccounts.Columns.Add("Type", "Loại tài khoản");
        _gridAccounts.Columns.Add("InitialBalance", "Số dư ban đầu");
        _gridAccounts.Columns.Add("CurrentBalance", "Số dư hiện tại");
        _gridAccounts.Columns.Add("Description", "Mô tả ghi chú");

        if (_gridAccounts.Columns["Name"] != null) _gridAccounts.Columns["Name"]!.Width = 200;
        if (_gridAccounts.Columns["Type"] != null) _gridAccounts.Columns["Type"]!.Width = 140;
        if (_gridAccounts.Columns["InitialBalance"] != null)
        {
            _gridAccounts.Columns["InitialBalance"]!.Width = 150;
            _gridAccounts.Columns["InitialBalance"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }
        if (_gridAccounts.Columns["CurrentBalance"] != null)
        {
            _gridAccounts.Columns["CurrentBalance"]!.Width = 160;
            _gridAccounts.Columns["CurrentBalance"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        _gridAccounts.CellDoubleClick += (s, e) => OnEditAccountClicked(s, e);

        _lblAccTotal = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            Font = Theme.FontAppBold,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "Tổng số dư: 0 VND"
        };

        grpGrid.Controls.Add(_gridAccounts);
        grpGrid.Controls.Add(_lblAccTotal);
        tab.Controls.Add(grpGrid);

        grpGrid.BringToFront();
        return tab;
    }
    #endregion

    #region Tab 4: Danh mục thu chi
    private TabPage CreateCategoriesTab()
    {
        var tab = new TabPage("Danh mục") { Padding = new Padding(12) };

        var pnlActions = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(0, 4, 0, 6) };

        pnlActions.Controls.Add(new Label { Text = "Phân loại:", AutoSize = true, Location = new Point(0, 10) });
        _cboCatFilter = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120, Location = new Point(65, 6) };
        _cboCatFilter.Items.Add(new ComboBoxItem("Tất cả", null!));
        _cboCatFilter.Items.Add(new ComboBoxItem("Chi tiêu", TransactionType.Expense));
        _cboCatFilter.Items.Add(new ComboBoxItem("Thu nhập", TransactionType.Income));
        _cboCatFilter.SelectedIndex = 0;
        _cboCatFilter.SelectedIndexChanged += (s, e) => LoadCategories();
        pnlActions.Controls.Add(_cboCatFilter);

        var btnAdd = new Button { Text = "+ Thêm danh mục", Width = 130, Height = 28, Location = new Point(200, 5) };
        btnAdd.Click += (s, e) => OpenAddCategory();

        var btnEdit = new Button { Text = "Sửa", Width = 70, Height = 28, Location = new Point(335, 5) };
        btnEdit.Click += OnEditCategoryClicked;

        var btnDelete = new Button { Text = "Xóa", Width = 70, Height = 28, Location = new Point(410, 5) };
        btnDelete.Click += OnDeleteCategoryClicked;

        pnlActions.Controls.Add(btnAdd);
        pnlActions.Controls.Add(btnEdit);
        pnlActions.Controls.Add(btnDelete);
        tab.Controls.Add(pnlActions);

        var grpGrid = new GroupBox { Text = "Danh sách danh mục phân loại thu chi", Dock = DockStyle.Fill, Padding = new Padding(10) };
        _gridCategories = new DataGridView { Dock = DockStyle.Fill };
        Theme.FormatGrid(_gridCategories);
        _gridCategories.Columns.Add("Id", "Mã");
        if (_gridCategories.Columns["Id"] != null) _gridCategories.Columns["Id"]!.Visible = false;

        _gridCategories.Columns.Add("Name", "Tên danh mục");
        _gridCategories.Columns.Add("Type", "Loại thu chi");
        _gridCategories.Columns.Add("Description", "Mô tả ghi chú");

        if (_gridCategories.Columns["Name"] != null) _gridCategories.Columns["Name"]!.Width = 220;
        if (_gridCategories.Columns["Type"] != null) _gridCategories.Columns["Type"]!.Width = 140;

        _gridCategories.CellDoubleClick += (s, e) => OnEditCategoryClicked(s, e);

        grpGrid.Controls.Add(_gridCategories);
        tab.Controls.Add(grpGrid);

        grpGrid.BringToFront();
        return tab;
    }
    #endregion

    #region Tab 5: Báo cáo & Thống kê
    private TabPage CreateReportsTab()
    {
        var tab = new TabPage("Báo cáo & Thống kê") { Padding = new Padding(12) };

        // Kỳ báo cáo
        var grpPeriod = new GroupBox { Text = "Chọn kỳ báo cáo", Dock = DockStyle.Top, Height = 65, Padding = new Padding(10) };
        var pnlPeriod = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };

        pnlPeriod.Controls.Add(new Label { Text = "Từ ngày:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
        _dtpRepFrom = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Width = 105, Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1) };
        _dtpRepFrom.ValueChanged += (s, e) => GenerateReport();
        pnlPeriod.Controls.Add(_dtpRepFrom);

        pnlPeriod.Controls.Add(new Label { Text = "  Đến ngày:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
        _dtpRepTo = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Width = 105, Value = DateTime.Today };
        _dtpRepTo.ValueChanged += (s, e) => GenerateReport();
        pnlPeriod.Controls.Add(_dtpRepTo);

        var btnThisMonth = new Button { Text = "Tháng này", Width = 85, Height = 26, Margin = new Padding(8, 2, 4, 0) };
        btnThisMonth.Click += (s, e) =>
        {
            _dtpRepFrom.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            _dtpRepTo.Value = DateTime.Today;
        };
        pnlPeriod.Controls.Add(btnThisMonth);

        var btnThisYear = new Button { Text = "Năm nay", Width = 85, Height = 26, Margin = new Padding(4, 2, 4, 0) };
        btnThisYear.Click += (s, e) =>
        {
            _dtpRepFrom.Value = new DateTime(DateTime.Today.Year, 1, 1);
            _dtpRepTo.Value = DateTime.Today;
        };
        pnlPeriod.Controls.Add(btnThisYear);

        grpPeriod.Controls.Add(pnlPeriod);
        tab.Controls.Add(grpPeriod);

        // Thống kê tóm tắt
        var grpSummary = new GroupBox { Text = "Kết quả tổng hợp trong kỳ", Dock = DockStyle.Top, Height = 80, Padding = new Padding(8) };
        var layoutSum = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1 };
        for (int i = 0; i < 4; i++) layoutSum.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

        layoutSum.Controls.Add(CreateStatBox("TỔNG THU KỲ NÀY", out _lblRepIncome, Theme.ColorIncome), 0, 0);
        layoutSum.Controls.Add(CreateStatBox("TỔNG CHI KỲ NÀY", out _lblRepExpense, Theme.ColorExpense), 1, 0);
        layoutSum.Controls.Add(CreateStatBox("TIẾT KIỆM THUẦN", out _lblRepSavings, SystemColors.HotTrack), 2, 0);
        layoutSum.Controls.Add(CreateStatBox("TỶ LỆ TIẾT KIỆM (%)", out _lblRepRate, Theme.ColorTransfer), 3, 0);

        grpSummary.Controls.Add(layoutSum);
        tab.Controls.Add(grpSummary);

        // 2 Bảng chi tiết
        var splitTables = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        splitTables.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        splitTables.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        var grpExp = new GroupBox { Text = "Cơ cấu chi tiêu theo từng danh mục", Dock = DockStyle.Fill, Padding = new Padding(8) };
        _gridRepExpenses = new DataGridView { Dock = DockStyle.Fill };
        Theme.FormatGrid(_gridRepExpenses);
        _gridRepExpenses.Columns.Add("Category", "Danh mục chi tiêu");
        _gridRepExpenses.Columns.Add("Amount", "Tổng tiền chi (VND)");
        _gridRepExpenses.Columns.Add("Percent", "Tỷ trọng (%)");
        if (_gridRepExpenses.Columns["Amount"] != null)
        {
            _gridRepExpenses.Columns["Amount"]!.Width = 140;
            _gridRepExpenses.Columns["Amount"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }
        if (_gridRepExpenses.Columns["Percent"] != null)
        {
            _gridRepExpenses.Columns["Percent"]!.Width = 100;
            _gridRepExpenses.Columns["Percent"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }
        grpExp.Controls.Add(_gridRepExpenses);

        var grpAcc = new GroupBox { Text = "Phân bổ số dư trên các ví tài khoản", Dock = DockStyle.Fill, Padding = new Padding(8) };
        _gridRepAccounts = new DataGridView { Dock = DockStyle.Fill };
        Theme.FormatGrid(_gridRepAccounts);
        _gridRepAccounts.Columns.Add("Account", "Tên ví / Tài khoản");
        _gridRepAccounts.Columns.Add("Balance", "Số dư hiện có (VND)");
        _gridRepAccounts.Columns.Add("Percent", "Tỷ trọng (%)");
        if (_gridRepAccounts.Columns["Balance"] != null)
        {
            _gridRepAccounts.Columns["Balance"]!.Width = 140;
            _gridRepAccounts.Columns["Balance"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }
        if (_gridRepAccounts.Columns["Percent"] != null)
        {
            _gridRepAccounts.Columns["Percent"]!.Width = 100;
            _gridRepAccounts.Columns["Percent"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }
        grpAcc.Controls.Add(_gridRepAccounts);

        splitTables.Controls.Add(grpExp, 0, 0);
        splitTables.Controls.Add(grpAcc, 1, 0);

        tab.Controls.Add(splitTables);
        splitTables.BringToFront();
        return tab;
    }
    #endregion

    #region Tab 6: Sao lưu & Khôi phục
    private TabPage CreateBackupTab()
    {
        var tab = new TabPage("Sao lưu & Khôi phục") { Padding = new Padding(16) };

        var grpCurrent = new GroupBox
        {
            Text = "Cơ sở dữ liệu SQLite hiện tại",
            Dock = DockStyle.Top,
            Height = 110,
            Padding = new Padding(12)
        };

        var lblPathTitle = new Label { Text = "Tập tin cơ sở dữ liệu trên máy tính:", AutoSize = true, Location = new Point(12, 25) };
        _txtDbPath = new TextBox { ReadOnly = true, Width = 650, Location = new Point(12, 48) };
        _lblDbSize = new Label { Text = "Kích thước: 0 KB", AutoSize = true, Location = new Point(675, 51) };

        grpCurrent.Controls.Add(lblPathTitle);
        grpCurrent.Controls.Add(_txtDbPath);
        grpCurrent.Controls.Add(_lblDbSize);
        tab.Controls.Add(grpCurrent);

        var grpActions = new GroupBox
        {
            Text = "Thao tác sao lưu và phục hồi dữ liệu an toàn",
            Dock = DockStyle.Top,
            Height = 220,
            Location = new Point(0, 120),
            Padding = new Padding(16)
        };

        var btnBackup = new Button
        {
            Text = "Sao lưu dữ liệu ra file (.db)...",
            Font = Theme.FontAppBold,
            Width = 240,
            Height = 42,
            Location = new Point(20, 35)
        };
        btnBackup.Click += (s, e) => PerformBackup();

        var lblBackupHelp = new Label
        {
            Text = "Tạo một bản sao hoàn chỉnh của toàn bộ tài khoản, danh mục và sổ giao dịch ra một tệp tin riêng biệt để lưu trữ an toàn.",
            Location = new Point(275, 42),
            Size = new Size(500, 36)
        };

        var btnRestore = new Button
        {
            Text = "Phục hồi dữ liệu từ file sao lưu...",
            Font = Theme.FontAppBold,
            Width = 240,
            Height = 42,
            Location = new Point(20, 105)
        };
        btnRestore.Click += (s, e) => PerformRestore();

        var lblRestoreHelp = new Label
        {
            Text = "Khôi phục dữ liệu từ tệp sao lưu đã tạo trước đó. Lưu ý: Dữ liệu hiện tại sẽ được thay thế bằng dữ liệu trong tệp sao lưu.",
            Location = new Point(275, 112),
            Size = new Size(500, 36)
        };

        grpActions.Controls.Add(btnBackup);
        grpActions.Controls.Add(lblBackupHelp);
        grpActions.Controls.Add(btnRestore);
        grpActions.Controls.Add(lblRestoreHelp);

        tab.Controls.Add(grpActions);
        return tab;
    }
    #endregion

    #region Data Loading & Refresh
    private void RefreshAllData()
    {
        var summary = _financeService.GetSummary();
        _statusBalance.Text = $"Tổng tài sản: {Theme.FormatMoney(summary.TotalAssets)}";

        // Tab 1: Overview
        _lblAssetValue.Text = Theme.FormatMoney(summary.TotalAssets);
        _lblIncomeValue.Text = $"+{Theme.FormatMoney(summary.TotalIncome)}";
        _lblExpenseValue.Text = $"-{Theme.FormatMoney(summary.TotalExpense)}";

        decimal net = summary.NetSavings;
        _lblSavingsValue.Text = (net >= 0 ? "+" : "") + Theme.FormatMoney(net);
        _lblSavingsValue.ForeColor = net >= 0 ? SystemColors.HotTrack : Theme.ColorExpense;

        LoadRecentGrid();
        LoadFilterAccounts();
        LoadTransactions();
        LoadAccounts();
        LoadCategories();
        GenerateReport();
        UpdateBackupTabInfo();
    }

    private void RefreshCurrentTab()
    {
        int idx = _tabControl.SelectedIndex;
        if (idx == 0) LoadRecentGrid();
        else if (idx == 1) LoadTransactions();
        else if (idx == 2) LoadAccounts();
        else if (idx == 3) LoadCategories();
        else if (idx == 4) GenerateReport();
        else if (idx == 5) UpdateBackupTabInfo();
    }

    private void LoadRecentGrid()
    {
        var list = _financeService.GetTransactions().Take(10).ToList();
        var accounts = _financeService.GetAllAccounts().ToDictionary(a => a.Id, a => a.Name);
        var categories = _financeService.GetAllCategories().ToDictionary(c => c.Id, c => c.Name);

        _gridRecent.Rows.Clear();
        foreach (var t in list)
        {
            string typeStr = t.Type switch
            {
                TransactionType.Income => "[Thu nhập]",
                TransactionType.Expense => "[Chi tiêu]",
                TransactionType.Transfer => "[Chuyển khoản]",
                _ => ""
            };

            string catName = t.CategoryId != null && categories.TryGetValue(t.CategoryId, out var cn) ? cn : "-";
            string accName = t.SourceAccountId != null && accounts.TryGetValue(t.SourceAccountId, out var an) ? an : "-";
            if (t.Type == TransactionType.Transfer && t.DestinationAccountId != null && accounts.TryGetValue(t.DestinationAccountId, out var dn))
            {
                accName = $"{accName} -> {dn}";
            }

            int idx = _gridRecent.Rows.Add(
                Theme.FormatDate(t.TransactionDate),
                typeStr,
                catName,
                accName,
                Theme.FormatMoney(t.Amount),
                t.Note
            );

            var row = _gridRecent.Rows[idx];
            if (t.Type == TransactionType.Income)
            {
                row.Cells["Amount"].Style.ForeColor = Theme.ColorIncome;
                row.Cells["Type"].Style.ForeColor = Theme.ColorIncome;
            }
            else if (t.Type == TransactionType.Expense)
            {
                row.Cells["Amount"].Style.ForeColor = Theme.ColorExpense;
                row.Cells["Type"].Style.ForeColor = Theme.ColorExpense;
            }
            else
            {
                row.Cells["Amount"].Style.ForeColor = Theme.ColorTransfer;
                row.Cells["Type"].Style.ForeColor = Theme.ColorTransfer;
            }
        }
    }

    private void LoadFilterAccounts()
    {
        var accounts = _financeService.GetAllAccounts();
        _cboTxAccount.Items.Clear();
        _cboTxAccount.Items.Add(new ComboBoxItem("Tất cả các ví", null!));
        foreach (var a in accounts)
        {
            _cboTxAccount.Items.Add(new ComboBoxItem(a.Name, a.Id));
        }
        if (_cboTxAccount.SelectedIndex < 0 && _cboTxAccount.Items.Count > 0)
            _cboTxAccount.SelectedIndex = 0;
    }

    private void LoadTransactions()
    {
        TransactionType? filterType = null;
        if (_cboTxType.SelectedItem is ComboBoxItem item && item.Value is TransactionType t)
            filterType = t;

        string? accountId = null;
        if (_cboTxAccount.SelectedItem is ComboBoxItem accItem && accItem.Value is string aid)
            accountId = aid;

        DateTime fromDate = _dtpTxFrom.Value.Date;
        DateTime toDate = _dtpTxTo.Value.Date.AddDays(1).AddTicks(-1);
        string search = _txtTxSearch.Text;

        var list = _financeService.GetTransactions(fromDate, toDate, accountId, null, filterType, search);
        var accounts = _financeService.GetAllAccounts().ToDictionary(a => a.Id, a => a.Name);
        var categories = _financeService.GetAllCategories().ToDictionary(c => c.Id, c => c.Name);

        _gridTransactions.Rows.Clear();
        decimal totalIn = 0;
        decimal totalOut = 0;

        foreach (var tx in list)
        {
            string typeStr = tx.Type switch
            {
                TransactionType.Income => "[Thu nhập]",
                TransactionType.Expense => "[Chi tiêu]",
                TransactionType.Transfer => "[Chuyển khoản]",
                _ => ""
            };

            string catName = tx.CategoryId != null && categories.TryGetValue(tx.CategoryId, out var cn) ? cn : "-";
            string srcAcc = tx.SourceAccountId != null && accounts.TryGetValue(tx.SourceAccountId, out var sn) ? sn : "-";
            string dstAcc = tx.DestinationAccountId != null && accounts.TryGetValue(tx.DestinationAccountId, out var dn) ? dn : "-";

            if (tx.Type == TransactionType.Income) totalIn += tx.Amount;
            if (tx.Type == TransactionType.Expense) totalOut += tx.Amount;

            int rowIdx = _gridTransactions.Rows.Add(
                tx.Id,
                Theme.FormatDate(tx.TransactionDate),
                typeStr,
                catName,
                srcAcc,
                dstAcc,
                tx.Note,
                Theme.FormatMoney(tx.Amount)
            );

            var row = _gridTransactions.Rows[rowIdx];
            if (tx.Type == TransactionType.Income)
            {
                row.Cells["Amount"].Style.ForeColor = Theme.ColorIncome;
                row.Cells["Type"].Style.ForeColor = Theme.ColorIncome;
            }
            else if (tx.Type == TransactionType.Expense)
            {
                row.Cells["Amount"].Style.ForeColor = Theme.ColorExpense;
                row.Cells["Type"].Style.ForeColor = Theme.ColorExpense;
            }
            else
            {
                row.Cells["Amount"].Style.ForeColor = Theme.ColorTransfer;
                row.Cells["Type"].Style.ForeColor = Theme.ColorTransfer;
            }
        }

        _lblTxSummary.Text = $"Tổng số giao dịch: {list.Count} | Thu: +{Theme.FormatMoney(totalIn)} | Chi: -{Theme.FormatMoney(totalOut)}";
    }

    private void LoadAccounts()
    {
        var accounts = _financeService.GetAllAccounts();
        _gridAccounts.Rows.Clear();
        decimal total = 0;

        foreach (var a in accounts)
        {
            total += a.CurrentBalance;
            string typeName = a.Type switch
            {
                AccountType.Cash => "Tiền mặt",
                AccountType.Bank => "Tài khoản ngân hàng",
                AccountType.CreditCard => "Thẻ tín dụng",
                AccountType.EWallet => "Ví điện tử",
                AccountType.Savings => "Sổ tiết kiệm",
                _ => a.Type.ToString()
            };

            int rowIdx = _gridAccounts.Rows.Add(
                a.Id,
                a.Name,
                typeName,
                Theme.FormatMoney(a.InitialBalance, a.Currency),
                Theme.FormatMoney(a.CurrentBalance, a.Currency),
                a.Description
            );

            var row = _gridAccounts.Rows[rowIdx];
            row.Cells["CurrentBalance"].Style.Font = Theme.FontAppBold;
            row.Cells["CurrentBalance"].Style.ForeColor = a.CurrentBalance >= 0 ? SystemColors.HotTrack : Theme.ColorExpense;
        }

        _lblAccTotal.Text = $"Tổng số dư khả dụng trên tất cả các ví: {Theme.FormatMoney(total)}";
    }

    private void LoadCategories()
    {
        TransactionType? filterType = null;
        if (_cboCatFilter.SelectedItem is ComboBoxItem item && item.Value is TransactionType t)
            filterType = t;

        var categories = _financeService.GetAllCategories(filterType);
        _gridCategories.Rows.Clear();

        foreach (var c in categories)
        {
            string typeStr = c.Type == TransactionType.Income ? "Thu nhập" : "Chi tiêu";
            int rowIdx = _gridCategories.Rows.Add(c.Id, c.Name, typeStr, c.Description);

            var row = _gridCategories.Rows[rowIdx];
            row.Cells["Type"].Style.ForeColor = c.Type == TransactionType.Income ? Theme.ColorIncome : Theme.ColorExpense;
            row.Cells["Type"].Style.Font = Theme.FontAppBold;
        }
    }

    private void GenerateReport()
    {
        DateTime fromDate = _dtpRepFrom.Value.Date;
        DateTime toDate = _dtpRepTo.Value.Date.AddDays(1).AddTicks(-1);

        var summary = _financeService.GetSummary(fromDate, toDate);

        _lblRepIncome.Text = Theme.FormatMoney(summary.TotalIncome);
        _lblRepExpense.Text = Theme.FormatMoney(summary.TotalExpense);

        decimal savings = summary.NetSavings;
        _lblRepSavings.Text = (savings >= 0 ? "+" : "") + Theme.FormatMoney(savings);
        _lblRepSavings.ForeColor = savings >= 0 ? SystemColors.HotTrack : Theme.ColorExpense;

        decimal rate = summary.TotalIncome > 0 ? (savings / summary.TotalIncome) * 100M : 0;
        _lblRepRate.Text = $"{rate:F1}%";

        // Grid Chi tiêu theo danh mục
        _gridRepExpenses.Rows.Clear();
        decimal totalExpense = summary.TotalExpense;
        foreach (var item in summary.ExpenseByCategory.OrderByDescending(c => c.Value))
        {
            decimal pct = totalExpense > 0 ? (item.Value / totalExpense) * 100M : 0;
            _gridRepExpenses.Rows.Add(item.Key, Theme.FormatMoney(item.Value), $"{pct:F1}%");
        }

        // Grid Tài khoản
        _gridRepAccounts.Rows.Clear();
        decimal totalAssets = summary.TotalAssets;
        foreach (var item in summary.BalanceByAccount.OrderByDescending(a => a.Value))
        {
            decimal pct = totalAssets > 0 ? (item.Value / totalAssets) * 100M : 0;
            _gridRepAccounts.Rows.Add(item.Key, Theme.FormatMoney(item.Value), $"{pct:F1}%");
        }
    }

    private void UpdateBackupTabInfo()
    {
        string path = _financeService.DatabasePath;
        _txtDbPath.Text = path;
        if (File.Exists(path))
        {
            long bytes = new FileInfo(path).Length;
            _lblDbSize.Text = $"Kích thước: {(bytes / 1024.0):F1} KB";
        }
        else
        {
            _lblDbSize.Text = "Kích thước: 0 KB";
        }
    }
    #endregion

    #region Action Handlers
    private void OpenAddTransaction(TransactionType defaultType = TransactionType.Expense)
    {
        using var dlg = new TransactionDialog(_financeService);
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.ResultTransaction != null)
        {
            _financeService.AddTransaction(dlg.ResultTransaction);
        }
    }

    private void OnEditTransactionClicked(object? sender, EventArgs e)
    {
        if (_gridTransactions.CurrentRow?.Cells["Id"]?.Value is not string id)
        {
            MessageBox.Show("Vui lòng chọn một giao dịch để chỉnh sửa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var tx = _financeService.GetTransactions().FirstOrDefault(t => t.Id == id);
        if (tx == null) return;

        using var dlg = new TransactionDialog(_financeService, tx);
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.ResultTransaction != null)
        {
            _financeService.UpdateTransaction(dlg.ResultTransaction);
        }
    }

    private void OnDeleteTransactionClicked(object? sender, EventArgs e)
    {
        if (_gridTransactions.CurrentRow?.Cells["Id"]?.Value is not string id)
        {
            MessageBox.Show("Vui lòng chọn một giao dịch cần xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show("Bạn có chắc chắn muốn xóa giao dịch này không?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm == DialogResult.Yes)
        {
            _financeService.DeleteTransaction(id);
        }
    }

    private void OpenAddAccount()
    {
        using var dlg = new AccountDialog();
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.ResultAccount != null)
        {
            _financeService.CreateAccount(dlg.ResultAccount);
        }
    }

    private void OnEditAccountClicked(object? sender, EventArgs e)
    {
        if (_gridAccounts.CurrentRow?.Cells["Id"]?.Value is not string id)
        {
            MessageBox.Show("Vui lòng chọn tài khoản cần sửa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var acc = _financeService.GetAllAccounts().FirstOrDefault(a => a.Id == id);
        if (acc == null) return;

        using var dlg = new AccountDialog(acc);
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.ResultAccount != null)
        {
            _financeService.UpdateAccount(dlg.ResultAccount);
        }
    }

    private void OnDeleteAccountClicked(object? sender, EventArgs e)
    {
        if (_gridAccounts.CurrentRow?.Cells["Id"]?.Value is not string id)
        {
            MessageBox.Show("Vui lòng chọn tài khoản cần xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show("Bạn có chắc chắn muốn xóa tài khoản/ví này?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm == DialogResult.Yes)
        {
            if (!_financeService.DeleteAccount(id, out string err))
            {
                MessageBox.Show(err, "Không thể xóa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    private void OpenAddCategory()
    {
        using var dlg = new CategoryDialog();
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.ResultCategory != null)
        {
            _financeService.CreateCategory(dlg.ResultCategory);
        }
    }

    private void OnEditCategoryClicked(object? sender, EventArgs e)
    {
        if (_gridCategories.CurrentRow?.Cells["Id"]?.Value is not string id)
        {
            MessageBox.Show("Vui lòng chọn danh mục cần sửa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var cat = _financeService.GetAllCategories().FirstOrDefault(c => c.Id == id);
        if (cat == null) return;

        using var dlg = new CategoryDialog(cat);
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.ResultCategory != null)
        {
            _financeService.UpdateCategory(dlg.ResultCategory);
        }
    }

    private void OnDeleteCategoryClicked(object? sender, EventArgs e)
    {
        if (_gridCategories.CurrentRow?.Cells["Id"]?.Value is not string id)
        {
            MessageBox.Show("Vui lòng chọn danh mục cần xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show("Bạn có chắc chắn muốn xóa danh mục này?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm == DialogResult.Yes)
        {
            if (!_financeService.DeleteCategory(id, out string err))
            {
                MessageBox.Show(err, "Không thể xóa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    private void OpenTransferDialog()
    {
        using var dlg = new TransferDialog(_financeService);
        dlg.ShowDialog(this);
    }

    private void PerformBackup()
    {
        using var sfd = new SaveFileDialog
        {
            Title = "Sao lưu cơ sở dữ liệu SQLite",
            Filter = "SQLite Database (*.db)|*.db|Tất cả tệp (*.*)|*.*",
            FileName = $"personal_finance_backup_{DateTime.Now:yyyyMMdd_HHmm}.db"
        };

        if (sfd.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                _financeService.BackupDatabase(sfd.FileName);
                MessageBox.Show($"Sao lưu cơ sở dữ liệu thành công ra tệp:\n{sfd.FileName}", "Sao lưu hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
                UpdateBackupTabInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi sao lưu dữ liệu: {ex.Message}", "Lỗi sao lưu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void PerformRestore()
    {
        using var ofd = new OpenFileDialog
        {
            Title = "Chọn tệp sao lưu SQLite để phục hồi",
            Filter = "SQLite Database (*.db)|*.db|Tất cả tệp (*.*)|*.*"
        };

        if (ofd.ShowDialog(this) == DialogResult.OK)
        {
            var confirm = MessageBox.Show(
                "Cảnh báo: Toàn bộ dữ liệu hiện tại sẽ được ghi đè bằng dữ liệu từ tệp sao lưu được chọn.\nBạn có muốn tiếp tục?",
                "Xác nhận phục hồi", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes)
            {
                try
                {
                    _financeService.RestoreDatabase(ofd.FileName);
                    MessageBox.Show("Phục hồi cơ sở dữ liệu thành công! Ứng dụng đã nạp lại toàn bộ dữ liệu.", "Phục hồi hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    RefreshAllData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi phục hồi dữ liệu: {ex.Message}", "Lỗi phục hồi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
    #endregion
}
