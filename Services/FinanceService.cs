using PersonalFinance.Data;
using PersonalFinance.Models;

namespace PersonalFinance.Services;

public class FinanceService : IFinanceService
{
    private readonly SqliteDatabase _db;

    public event EventHandler? DataChanged;

    public string DatabasePath => _db.DatabasePath;

    public FinanceService(SqliteDatabase db)
    {
        _db = db;
    }

    public List<Account> GetAllAccounts() => _db.GetAccounts();

    public void CreateAccount(Account a)
    {
        if (string.IsNullOrWhiteSpace(a.Name))
            throw new ArgumentException("Tên tài khoản không được để trống.");

        a.CurrentBalance = a.InitialBalance;
        _db.InsertAccount(a);
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateAccount(Account a)
    {
        if (string.IsNullOrWhiteSpace(a.Name))
            throw new ArgumentException("Tên tài khoản không được để trống.");

        _db.UpdateAccount(a);
        _db.RecalculateBalances();
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool DeleteAccount(string id, out string error)
    {
        bool success = _db.DeleteAccount(id, out error);
        if (success)
        {
            DataChanged?.Invoke(this, EventArgs.Empty);
        }
        return success;
    }

    public List<Category> GetAllCategories(TransactionType? type = null) => _db.GetCategories(type);

    public void CreateCategory(Category c)
    {
        if (string.IsNullOrWhiteSpace(c.Name))
            throw new ArgumentException("Tên danh mục không được để trống.");

        _db.InsertCategory(c);
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateCategory(Category c)
    {
        if (string.IsNullOrWhiteSpace(c.Name))
            throw new ArgumentException("Tên danh mục không được để trống.");

        _db.UpdateCategory(c);
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool DeleteCategory(string id, out string error)
    {
        bool success = _db.DeleteCategory(id, out error);
        if (success)
        {
            DataChanged?.Invoke(this, EventArgs.Empty);
        }
        return success;
    }

    public List<Transaction> GetTransactions(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? accountId = null,
        string? categoryId = null,
        TransactionType? type = null,
        string? keyword = null)
    {
        var query = _db.GetTransactions().AsEnumerable();

        if (fromDate.HasValue)
            query = query.Where(t => t.TransactionDate.Date >= fromDate.Value.Date);

        if (toDate.HasValue)
            query = query.Where(t => t.TransactionDate.Date <= toDate.Value.Date);

        if (!string.IsNullOrEmpty(accountId))
            query = query.Where(t => t.SourceAccountId == accountId || t.DestinationAccountId == accountId);

        if (!string.IsNullOrEmpty(categoryId))
            query = query.Where(t => t.CategoryId == categoryId);

        if (type.HasValue)
            query = query.Where(t => t.Type == type.Value);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            string kw = keyword.Trim().ToLowerInvariant();
            query = query.Where(t => t.Note.ToLowerInvariant().Contains(kw));
        }

        return query.OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.CreatedAt).ToList();
    }

    public void AddTransaction(Transaction t)
    {
        if (t.Amount <= 0)
            throw new ArgumentException("Số tiền giao dịch phải lớn hơn 0.");

        _db.InsertTransaction(t);
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateTransaction(Transaction t)
    {
        if (t.Amount <= 0)
            throw new ArgumentException("Số tiền giao dịch phải lớn hơn 0.");

        _db.UpdateTransaction(t);
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public void DeleteTransaction(string id)
    {
        _db.DeleteTransaction(id);
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public void TransferFunds(string fromAccountId, string toAccountId, decimal amount, DateTime date, string note)
    {
        if (fromAccountId == toAccountId)
            throw new ArgumentException("Ví gửi và ví nhận phải khác nhau.");

        if (amount <= 0)
            throw new ArgumentException("Số tiền chuyển phải lớn hơn 0.");

        var tx = new Transaction
        {
            Id = Guid.NewGuid().ToString("N"),
            TransactionDate = date,
            Amount = amount,
            Type = TransactionType.Transfer,
            SourceAccountId = fromAccountId,
            DestinationAccountId = toAccountId,
            Note = string.IsNullOrWhiteSpace(note) ? "Chuyển tiền giữa các ví" : note,
            CreatedAt = DateTime.Now
        };

        AddTransaction(tx);
    }

    public FinancialSummary GetSummary(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var accounts = _db.GetAccounts();
        var allTx = GetTransactions(fromDate, toDate);
        var categories = _db.GetCategories().ToDictionary(c => c.Id, c => c.Name);

        var summary = new FinancialSummary
        {
            TotalAssets = accounts.Sum(a => a.CurrentBalance),
            TotalIncome = allTx.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
            TotalExpense = allTx.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)
        };

        foreach (var acc in accounts)
        {
            summary.BalanceByAccount[acc.Name] = acc.CurrentBalance;
        }

        var expenseTx = allTx.Where(t => t.Type == TransactionType.Expense);
        foreach (var group in expenseTx.GroupBy(t => t.CategoryId ?? "Khác"))
        {
            string catName = categories.TryGetValue(group.Key, out var name) ? name : "Khác";
            summary.ExpenseByCategory[catName] = group.Sum(t => t.Amount);
        }

        return summary;
    }

    public void BackupDatabase(string targetPath)
    {
        _db.BackupDatabase(targetPath);
    }

    public void RestoreDatabase(string sourcePath)
    {
        _db.RestoreDatabase(sourcePath);
        DataChanged?.Invoke(this, EventArgs.Empty);
    }
}
