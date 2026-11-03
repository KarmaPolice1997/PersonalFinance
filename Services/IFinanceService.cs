using PersonalFinance.Models;

namespace PersonalFinance.Services;

public interface IFinanceService
{
    event EventHandler? DataChanged;

    // Accounts
    IReadOnlyList<Account> GetAllAccounts();
    Account? GetAccount(string id);
    void CreateAccount(Account account);
    void UpdateAccount(Account account);
    bool DeleteAccount(string id, out string errorMessage);

    // Categories
    IReadOnlyList<Category> GetAllCategories(TransactionType? type = null);
    Category? GetCategory(string id);
    void CreateCategory(Category category);
    void UpdateCategory(Category category);
    bool DeleteCategory(string id, out string errorMessage);

    // Transactions
    IReadOnlyList<Transaction> GetTransactions(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? accountId = null,
        string? categoryId = null,
        TransactionType? type = null,
        string? keyword = null);

    void AddTransaction(Transaction transaction);
    void UpdateTransaction(Transaction original, Transaction updated);
    void DeleteTransaction(string transactionId);
    void TransferFunds(string fromAccountId, string toAccountId, decimal amount, DateTime date, string note);

    // Analytics & Reports
    FinancialSummary GetSummary(DateTime? fromDate = null, DateTime? toDate = null);
    void RefreshData();
}
