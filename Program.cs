using PersonalFinance.Services;
using PersonalFinance.UI;

namespace PersonalFinance;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Configure default application font to Segoe UI
        Application.SetDefaultFont(Theme.FontBody);

        // Composition Root: Dependency Injection wiring
        IDataContext dataContext = new JsonDataContext();
        IFinanceService financeService = new FinanceService(dataContext);

        Application.Run(new MainForm(financeService));
    }
}
