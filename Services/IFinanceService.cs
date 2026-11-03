using PersonalFinance.Models;

namespace PersonalFinance.Services;

public interface IFinanceService
{
    event EventHandler? DataChanged;

    string DatabasePath { get; }

    // Tài khoản
    List<Account> GetAllAccounts();
    void CreateAccount(Account a);
    void UpdateAccount(Account a);
    bool DeleteAccount(string id, out string error);

    // Danh mục
    List<Category> GetAllCategories(TransactionType? type = null);
    void CreateCategory(Category c);
    void UpdateCategory(Category c);
    bool DeleteCategory(string id, out string error);

    // Giao dịch
    List<Transaction> GetTransactions(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? accountId = null,
        string? categoryId = null,
        TransactionType? type = null,
        string? keyword = null);

    void AddTransaction(Transaction t);
    void UpdateTransaction(Transaction t);
    void DeleteTransaction(string id);
    void TransferFunds(string fromAccountId, string toAccountId, decimal amount, DateTime date, string note);

    // Thống kê
    FinancialSummary GetSummary(DateTime? fromDate = null, DateTime? toDate = null);

    // Sao lưu & Phục hồi
    void BackupDatabase(string targetPath);
    void RestoreDatabase(string sourcePath);
}
