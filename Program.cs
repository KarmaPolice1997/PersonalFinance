using PersonalFinance.Data;
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

        // Cấu hình font mặc định chuẩn Segoe UI cho toàn bộ ứng dụng
        Application.SetDefaultFont(Theme.FontApp);

        // Khởi tạo tầng cơ sở dữ liệu SQLite và tầng dịch vụ nghiệp vụ
        using var sqliteDb = new SqliteDatabase();
        IFinanceService financeService = new FinanceService(sqliteDb);

        Application.Run(new MainForm(financeService));
    }
}
