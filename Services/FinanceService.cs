using PersonalFinance.Models;

namespace PersonalFinance.Services;

public class FinanceService : IFinanceService
{
    private readonly IDataContext _dataContext;

    public event EventHandler? DataChanged;

    public FinanceService(IDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public IReadOnlyList<Account> GetAllAccounts()
    {
        return _dataContext.Accounts.GetAll().OrderBy(a => a.Name).ToList();
    }

    public Account? GetAccount(string id)
    {
        return _dataContext.Accounts.GetById(id);
    }

    public void CreateAccount(Account account)
    {
        ArgumentNullException.ThrowIfNull(account);
        if (string.IsNullOrWhiteSpace(account.Name))
            throw new ArgumentException("Ten tai khoan khong duoc de trong.");

        account.CurrentBalance = account.InitialBalance;
        _dataContext.Accounts.Add(account);
        _dataContext.SaveChanges();
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateAccount(Account account)
    {
        ArgumentNullException.ThrowIfNull(account);
        _dataContext.Accounts.Update(account);
        RecalculateBalances();
        _dataContext.SaveChanges();
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool DeleteAccount(string id, out string errorMessage)
    {
        var hasTransactions = _dataContext.Transactions.GetAll()
            .Any(t => t.SourceAccountId == id || t.DestinationAccountId == id);

        if (hasTransactions)
        {
            errorMessage = "Khong the xoa tai khoan dang co lich su giao dich. Hay xoa hoac doi giao dich truoc.";
            return false;
        }

        var deleted = _dataContext.Accounts.Delete(id);
        if (deleted)
        {
            _dataContext.SaveChanges();
            DataChanged?.Invoke(this, EventArgs.Empty);
            errorMessage = string.Empty;
            return true;
        }

        errorMessage = "Khong tim thay tai khoan de xoa.";
        return false;
    }

    public IReadOnlyList<Category> GetAllCategories(TransactionType? type = null)
    {
        var query = _dataContext.Categories.GetAll().AsEnumerable();
        if (type.HasValue)
        {
            query = query.Where(c => c.Type == type.Value);
        }
        return query.OrderBy(c => c.Name).ToList();
    }

    public Category? GetCategory(string id)
    {
        return _dataContext.Categories.GetById(id);
    }

    public void CreateCategory(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);
        if (string.IsNullOrWhiteSpace(category.Name))
            throw new ArgumentException("Ten danh muc khong duoc de trong.");

        _dataContext.Categories.Add(category);
        _dataContext.SaveChanges();
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateCategory(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);
        _dataContext.Categories.Update(category);
        _dataContext.SaveChanges();
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool DeleteCategory(string id, out string errorMessage)
    {
        var hasTransactions = _dataContext.Transactions.GetAll().Any(t => t.CategoryId == id);
        if (hasTransactions)
        {
            errorMessage = "Khong the xoa danh muc dang duoc su dung trong giao dich.";
            return false;
        }

        var deleted = _dataContext.Categories.Delete(id);
        if (deleted)
        {
            _dataContext.SaveChanges();
            DataChanged?.Invoke(this, EventArgs.Empty);
            errorMessage = string.Empty;
            return true;
        }

        errorMessage = "Khong tim thay danh muc can xoa.";
        return false;
    }

    public IReadOnlyList<Transaction> GetTransactions(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? accountId = null,
        string? categoryId = null,
        TransactionType? type = null,
        string? keyword = null)
    {
        var query = _dataContext.Transactions.GetAll().AsEnumerable();

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
            var kw = keyword.Trim().ToLowerInvariant();
            query = query.Where(t => (t.Note ?? string.Empty).ToLowerInvariant().Contains(kw));
        }

        return query.OrderByDescending(t => t.TransactionDate)
                    .ThenByDescending(t => t.CreatedAt)
                    .ToList();
    }

    public void AddTransaction(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        if (transaction.Amount <= 0)
            throw new ArgumentException("So tien giao dich phai lon hon 0.");

        _dataContext.Transactions.Add(transaction);
        RecalculateBalances();
        _dataContext.SaveChanges();
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateTransaction(Transaction original, Transaction updated)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(updated);

        _dataContext.Transactions.Update(updated);
        RecalculateBalances();
        _dataContext.SaveChanges();
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public void DeleteTransaction(string transactionId)
    {
        var deleted = _dataContext.Transactions.Delete(transactionId);
        if (deleted)
        {
            RecalculateBalances();
            _dataContext.SaveChanges();
            DataChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void TransferFunds(string fromAccountId, string toAccountId, decimal amount, DateTime date, string note)
    {
        if (fromAccountId == toAccountId)
            throw new ArgumentException("Tai khoan nguon va tai khoan dich phai khac nhau.");

        if (amount <= 0)
            throw new ArgumentException("So tien chuyen phai lon hon 0.");

        var tx = new Transaction
        {
            TransactionDate = date,
            Amount = amount,
            Type = TransactionType.Transfer,
            SourceAccountId = fromAccountId,
            DestinationAccountId = toAccountId,
            Note = string.IsNullOrWhiteSpace(note) ? "Chuyen khoan giua cac vi" : note
        };

        AddTransaction(tx);
    }

    public FinancialSummary GetSummary(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var accounts = _dataContext.Accounts.GetAll();
        var allTx = GetTransactions(fromDate, toDate);
        var categories = _dataContext.Categories.GetAll().ToDictionary(c => c.Id, c => c.Name);

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
        foreach (var group in expenseTx.GroupBy(t => t.CategoryId ?? "Khac"))
        {
            var catName = categories.TryGetValue(group.Key, out var name) ? name : "Khac";
            summary.ExpenseByCategory[catName] = group.Sum(t => t.Amount);
        }

        return summary;
    }

    public void RefreshData()
    {
        _dataContext.Reload();
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RecalculateBalances()
    {
        var accounts = _dataContext.Accounts.GetAll().ToDictionary(a => a.Id);
        foreach (var acc in accounts.Values)
        {
            acc.CurrentBalance = acc.InitialBalance;
        }

        foreach (var tx in _dataContext.Transactions.GetAll().OrderBy(t => t.TransactionDate).ThenBy(t => t.CreatedAt))
        {
            if (tx.Type == TransactionType.Income && tx.SourceAccountId != null && accounts.TryGetValue(tx.SourceAccountId, out var targetAcc))
            {
                targetAcc.CurrentBalance += tx.Amount;
            }
            else if (tx.Type == TransactionType.Expense && tx.SourceAccountId != null && accounts.TryGetValue(tx.SourceAccountId, out var srcAcc))
            {
                srcAcc.CurrentBalance -= tx.Amount;
            }
            else if (tx.Type == TransactionType.Transfer)
            {
                if (tx.SourceAccountId != null && accounts.TryGetValue(tx.SourceAccountId, out var fromAcc))
                {
                    fromAcc.CurrentBalance -= tx.Amount;
                }
                if (tx.DestinationAccountId != null && accounts.TryGetValue(tx.DestinationAccountId, out var toAcc))
                {
                    toAcc.CurrentBalance += tx.Amount;
                }
            }
        }
    }
}
