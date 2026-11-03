using PersonalFinance.Services;
using PersonalFinance.UI.Views;

namespace PersonalFinance.UI;

public partial class MainForm : Form
{
    private readonly IFinanceService _financeService;

    private Panel _pnlSidebar = null!;
    private Panel _pnlHeader = null!;
    private Panel _pnlContent = null!;
    private Label _lblHeaderTitle = null!;
    private Label _lblHeaderBalance = null!;

    private readonly List<Button> _navButtons = new();

    private DashboardView _dashboardView = null!;
    private TransactionsView _transactionsView = null!;
    private AccountsView _accountsView = null!;
    private CategoriesView _categoriesView = null!;
    private ReportsView _reportsView = null!;

    public MainForm(IFinanceService financeService)
    {
        _financeService = financeService;
        InitializeComponent();

        _financeService.DataChanged += (s, e) =>
        {
            if (IsHandleCreated && !IsDisposed)
            {
                BeginInvoke(new Action(UpdateHeaderBalance));
            }
        };

        UpdateHeaderBalance();
    }

    private void InitializeComponent()
    {
        Text = "Phan mem Quan ly Tai chinh Ca nhan (UML Architecture)";
        Font = Theme.FontBody;
        BackColor = Theme.BgApp;
        ForeColor = Theme.TextPrimary;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1180, 720);
        Size = new Size(1280, 780);

        // Sidebar Panel
        _pnlSidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 230,
            BackColor = Theme.SidebarBg,
            Padding = new Padding(0)
        };

        // App Brand in Sidebar
        var pnlBrand = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Theme.SidebarBg,
            Padding = new Padding(20, 16, 16, 10)
        };

        var lblBrand = new Label
        {
            Text = "PERSONAL FINANCE",
            Font = Theme.FontAppTitle,
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            Height = 24
        };

        var lblSub = new Label
        {
            Text = "UML WinForms Edition",
            Font = Theme.FontSmall,
            ForeColor = Theme.SidebarText,
            Dock = DockStyle.Top,
            Height = 18
        };

        pnlBrand.Controls.Add(lblSub);
        pnlBrand.Controls.Add(lblBrand);
        _pnlSidebar.Controls.Add(pnlBrand);

        // Navigation Buttons
        var pnlNav = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 10, 12, 10)
        };

        AddNavButton(pnlNav, "Tong quan", 0, () => ShowView(_dashboardView, "Tong quan tai chinh"));
        AddNavButton(pnlNav, "Giao dich", 1, () => ShowView(_transactionsView, "So giao dich thu chi"));
        AddNavButton(pnlNav, "Tai khoan / Vi", 2, () => ShowView(_accountsView, "Quan ly tai khoan & vi"));
        AddNavButton(pnlNav, "Danh muc", 3, () => ShowView(_categoriesView, "Danh muc phan loai thu chi"));
        AddNavButton(pnlNav, "Bao cao thong ke", 4, () => ShowView(_reportsView, "Bao cao & phan tich"));

        _pnlSidebar.Controls.Add(pnlNav);

        // Header Panel
        _pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 65,
            BackColor = Theme.BgCard,
            Padding = new Padding(24, 0, 24, 0)
        };

        _lblHeaderTitle = new Label
        {
            Text = "Tong quan tai chinh",
            Font = Theme.FontPageTitle,
            ForeColor = Theme.TextPrimary,
            AutoSize = false,
            Dock = DockStyle.Left,
            Width = 400,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var pnlRightHeader = new Panel
        {
            Dock = DockStyle.Right,
            Width = 350,
            Padding = new Padding(0, 12, 0, 12)
        };

        var lblToday = new Label
        {
            Text = $"Ngay: {Theme.FormatDate(DateTime.Today)}",
            Font = Theme.FontSmall,
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleRight,
            Height = 18
        };

        _lblHeaderBalance = new Label
        {
            Text = "Kha dung: 0 VND",
            Font = Theme.FontBodyBold,
            ForeColor = Theme.Primary,
            Dock = DockStyle.Bottom,
            TextAlign = ContentAlignment.MiddleRight,
            Height = 22
        };

        pnlRightHeader.Controls.Add(lblToday);
        pnlRightHeader.Controls.Add(_lblHeaderBalance);

        _pnlHeader.Controls.Add(_lblHeaderTitle);
        _pnlHeader.Controls.Add(pnlRightHeader);

        // Border line under header
        var pnlHeaderBorder = new Panel
        {
            Dock = DockStyle.Top,
            Height = 1,
            BackColor = Theme.BorderCard
        };

        // Main Content Host Panel
        _pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.BgApp
        };

        Controls.Add(_pnlContent);
        Controls.Add(pnlHeaderBorder);
        Controls.Add(_pnlHeader);
        Controls.Add(_pnlSidebar);

        // Initialize Views
        _dashboardView = new DashboardView(_financeService);
        _transactionsView = new TransactionsView(_financeService);
        _accountsView = new AccountsView(_financeService);
        _categoriesView = new CategoriesView(_financeService);
        _reportsView = new ReportsView(_financeService);

        // Default View
        SetActiveNavButton(0);
        ShowView(_dashboardView, "Tong quan tai chinh");
    }

    private void AddNavButton(Panel container, string text, int index, Action onClick)
    {
        var btn = new Button
        {
            Text = text,
            Font = Theme.FontBody,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(14, 0, 0, 0),
            Height = 44,
            Dock = DockStyle.Top,
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.SidebarBg,
            ForeColor = Theme.SidebarText,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 4, 0, 4)
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = Theme.SidebarHover;

        btn.Click += (s, e) =>
        {
            SetActiveNavButton(index);
            onClick();
        };

        // Add in reverse because Dock = Top stacks them
        _navButtons.Add(btn);
        container.Controls.Add(btn);
        btn.BringToFront();
    }

    private void SetActiveNavButton(int activeIndex)
    {
        for (int i = 0; i < _navButtons.Count; i++)
        {
            if (i == activeIndex)
            {
                _navButtons[i].BackColor = Theme.SidebarActive;
                _navButtons[i].ForeColor = Theme.SidebarTextActive;
                _navButtons[i].Font = Theme.FontBodyBold;
            }
            else
            {
                _navButtons[i].BackColor = Theme.SidebarBg;
                _navButtons[i].ForeColor = Theme.SidebarText;
                _navButtons[i].Font = Theme.FontBody;
            }
        }
    }

    private void ShowView(UserControl view, string title)
    {
        _lblHeaderTitle.Text = title;
        _pnlContent.Controls.Clear();
        view.Dock = DockStyle.Fill;
        _pnlContent.Controls.Add(view);

        if (view is DashboardView db) db.LoadDashboardData();
        else if (view is TransactionsView tx) tx.LoadTransactions();
        else if (view is AccountsView acc) acc.LoadAccounts();
        else if (view is CategoriesView cat) cat.LoadCategories();
        else if (view is ReportsView rep) rep.GenerateReport();
    }

    private void UpdateHeaderBalance()
    {
        var summary = _financeService.GetSummary();
        _lblHeaderBalance.Text = $"Kha dung: {Theme.FormatMoney(summary.TotalAssets)}";
    }
}
