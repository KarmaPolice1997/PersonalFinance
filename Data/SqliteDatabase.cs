using System.Runtime.InteropServices;
using System.Text;
using PersonalFinance.Models;

namespace PersonalFinance.Data;

public class SqliteDatabase : IDisposable
{
    private const string DllName = "winsqlite3.dll";
    private const int SQLITE_OK = 0;
    private const int SQLITE_ROW = 100;
    private const int SQLITE_DONE = 101;

    [DllImport(DllName, EntryPoint = "sqlite3_open16", CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_open16([MarshalAs(UnmanagedType.LPWStr)] string filename, out IntPtr db);

    [DllImport(DllName, EntryPoint = "sqlite3_close", CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_close(IntPtr db);

    [DllImport(DllName, EntryPoint = "sqlite3_exec", CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_exec(IntPtr db, byte[] sql, IntPtr callback, IntPtr arg, out IntPtr errMsg);

    [DllImport(DllName, EntryPoint = "sqlite3_prepare16_v2", CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_prepare16_v2(IntPtr db, [MarshalAs(UnmanagedType.LPWStr)] string zSql, int nByte, out IntPtr ppStmt, IntPtr pzTail);

    [DllImport(DllName, EntryPoint = "sqlite3_step", CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_step(IntPtr pStmt);

    [DllImport(DllName, EntryPoint = "sqlite3_finalize", CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_finalize(IntPtr pStmt);

    [DllImport(DllName, EntryPoint = "sqlite3_bind_text16", CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_bind_text16(IntPtr pStmt, int index, [MarshalAs(UnmanagedType.LPWStr)] string val, int nByte, IntPtr destructor);

    [DllImport(DllName, EntryPoint = "sqlite3_bind_double", CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_bind_double(IntPtr pStmt, int index, double val);

    [DllImport(DllName, EntryPoint = "sqlite3_bind_int", CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_bind_int(IntPtr pStmt, int index, int val);

    [DllImport(DllName, EntryPoint = "sqlite3_bind_null", CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_bind_null(IntPtr pStmt, int index);

    [DllImport(DllName, EntryPoint = "sqlite3_column_text16", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr sqlite3_column_text16(IntPtr pStmt, int col);

    [DllImport(DllName, EntryPoint = "sqlite3_column_double", CallingConvention = CallingConvention.Cdecl)]
    private static extern double sqlite3_column_double(IntPtr pStmt, int col);

    [DllImport(DllName, EntryPoint = "sqlite3_column_int", CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_column_int(IntPtr pStmt, int col);

    [DllImport(DllName, EntryPoint = "sqlite3_errmsg16", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr sqlite3_errmsg16(IntPtr db);

    private IntPtr _db = IntPtr.Zero;
    private readonly string _dbPath;

    public string DatabasePath => _dbPath;

    public SqliteDatabase(string? dbPath = null)
    {
        _dbPath = dbPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "personal_finance.db");
        Open();
        InitializeSchema();
        SeedIfEmpty();
    }

    private void Open()
    {
        if (_db != IntPtr.Zero) return;
        int rc = sqlite3_open16(_dbPath, out _db);
        if (rc != SQLITE_OK)
        {
            throw new InvalidOperationException($"Khong the mo co so du lieu SQLite: {GetLastErrorMessage()}");
        }
    }

    public void Close()
    {
        if (_db != IntPtr.Zero)
        {
            sqlite3_close(_db);
            _db = IntPtr.Zero;
        }
    }

    private string GetLastErrorMessage()
    {
        if (_db == IntPtr.Zero) return "Co so du lieu chua duoc khoi tao.";
        var ptr = sqlite3_errmsg16(_db);
        return Marshal.PtrToStringUni(ptr) ?? "Loi khong xac dinh";
    }

    public void ExecuteNonQuery(string sql)
    {
        Open();
        byte[] utf8 = Encoding.UTF8.GetBytes(sql + "\0");
        int rc = sqlite3_exec(_db, utf8, IntPtr.Zero, IntPtr.Zero, out var errMsgPtr);
        if (rc != SQLITE_OK)
        {
            string err = errMsgPtr != IntPtr.Zero ? Marshal.PtrToStringAnsi(errMsgPtr) ?? "Loi SQL" : GetLastErrorMessage();
            throw new InvalidOperationException($"Loi thuc thi SQL: {err}\nQuery: {sql}");
        }
    }

    private void InitializeSchema()
    {
        string ddl = @"
            CREATE TABLE IF NOT EXISTS accounts (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                type INTEGER NOT NULL,
                initial_balance REAL NOT NULL,
                current_balance REAL NOT NULL,
                currency TEXT NOT NULL,
                description TEXT
            );

            CREATE TABLE IF NOT EXISTS categories (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                type INTEGER NOT NULL,
                description TEXT
            );

            CREATE TABLE IF NOT EXISTS transactions (
                id TEXT PRIMARY KEY,
                tx_date TEXT NOT NULL,
                amount REAL NOT NULL,
                type INTEGER NOT NULL,
                category_id TEXT,
                source_account_id TEXT,
                dest_account_id TEXT,
                note TEXT,
                created_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS settings (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );
        ";
        ExecuteNonQuery(ddl);
    }

    private void SeedIfEmpty()
    {
        var accounts = GetAccounts();
        if (accounts.Count > 0) return;

        // Seed default accounts
        InsertAccount(new Account { Id = "acc_cash", Name = "Ví tiền mặt", Type = AccountType.Cash, InitialBalance = 3000000, CurrentBalance = 3000000, Description = "Tiêu dùng hằng ngày" });
        InsertAccount(new Account { Id = "acc_bank", Name = "Ngân hàng Vietcombank", Type = AccountType.Bank, InitialBalance = 25000000, CurrentBalance = 25000000, Description = "Nhận lương & Thanh toán" });
        InsertAccount(new Account { Id = "acc_momo", Name = "Ví MoMo", Type = AccountType.EWallet, InitialBalance = 1500000, CurrentBalance = 1500000, Description = "Thanh toán hoá đơn" });
        InsertAccount(new Account { Id = "acc_save", Name = "Sổ tiết kiệm", Type = AccountType.Savings, InitialBalance = 50000000, CurrentBalance = 50000000, Description = "Quỹ dự phòng dài hạn" });

        // Seed default categories
        InsertCategory(new Category { Id = "cat_food", Name = "Ăn uống & Cà phê", Type = TransactionType.Expense, Description = "Bữa ăn, tiệc tùng, cà phê" });
        InsertCategory(new Category { Id = "cat_rent", Name = "Tiền nhà & Điện nước", Type = TransactionType.Expense, Description = "Sinh hoạt phí cố định" });
        InsertCategory(new Category { Id = "cat_trans", Name = "Xăng xe & Đi lại", Type = TransactionType.Expense, Description = "Bảo dưỡng, đổ xăng, xe công nghệ" });
        InsertCategory(new Category { Id = "cat_shop", Name = "Mua sắm cá nhân", Type = TransactionType.Expense, Description = "Quần áo, đồ dùng gia đình" });
        InsertCategory(new Category { Id = "cat_health", Name = "Y tế & Sức khỏe", Type = TransactionType.Expense, Description = "Thuốc men, khám sức khỏe" });

        InsertCategory(new Category { Id = "cat_salary", Name = "Lương hàng tháng", Type = TransactionType.Income, Description = "Thu nhập chính" });
        InsertCategory(new Category { Id = "cat_bonus", Name = "Thưởng & Dự án", Type = TransactionType.Income, Description = "Thu nhập ngoài giờ" });
        InsertCategory(new Category { Id = "cat_invest", Name = "Lãi suất & Đầu tư", Type = TransactionType.Income, Description = "Cổ tức, lãi ngân hàng" });

        // Seed transactions
        DateTime today = DateTime.Today;
        InsertTransaction(new Transaction { Id = "tx_1", TransactionDate = today.AddDays(-5), Amount = 20000000, Type = TransactionType.Income, CategoryId = "cat_salary", SourceAccountId = "acc_bank", Note = "Lương công ty" });
        InsertTransaction(new Transaction { Id = "tx_2", TransactionDate = today.AddDays(-4), Amount = 4500000, Type = TransactionType.Expense, CategoryId = "cat_rent", SourceAccountId = "acc_bank", Note = "Tiền nhà tháng" });
        InsertTransaction(new Transaction { Id = "tx_3", TransactionDate = today.AddDays(-3), Amount = 1000000, Type = TransactionType.Transfer, SourceAccountId = "acc_bank", DestinationAccountId = "acc_cash", Note = "Rút tiền mặt chi tiêu" });
        InsertTransaction(new Transaction { Id = "tx_4", TransactionDate = today.AddDays(-2), Amount = 250000, Type = TransactionType.Expense, CategoryId = "cat_food", SourceAccountId = "acc_cash", Note = "Ăn trưa bạn bè" });
        InsertTransaction(new Transaction { Id = "tx_5", TransactionDate = today.AddDays(-1), Amount = 80000, Type = TransactionType.Expense, CategoryId = "cat_trans", SourceAccountId = "acc_cash", Note = "Đổ xăng xe máy" });

        RecalculateBalances();
    }

    #region Accounts CRUD
    public List<Account> GetAccounts()
    {
        Open();
        var list = new List<Account>();
        string sql = "SELECT id, name, type, initial_balance, current_balance, currency, description FROM accounts ORDER BY name;";
        int rc = sqlite3_prepare16_v2(_db, sql, -1, out var stmt, IntPtr.Zero);
        if (rc != SQLITE_OK) return list;

        try
        {
            while (sqlite3_step(stmt) == SQLITE_ROW)
            {
                list.Add(new Account
                {
                    Id = Marshal.PtrToStringUni(sqlite3_column_text16(stmt, 0)) ?? "",
                    Name = Marshal.PtrToStringUni(sqlite3_column_text16(stmt, 1)) ?? "",
                    Type = (AccountType)sqlite3_column_int(stmt, 2),
                    InitialBalance = (decimal)sqlite3_column_double(stmt, 3),
                    CurrentBalance = (decimal)sqlite3_column_double(stmt, 4),
                    Currency = Marshal.PtrToStringUni(sqlite3_column_text16(stmt, 5)) ?? "VND",
                    Description = Marshal.PtrToStringUni(sqlite3_column_text16(stmt, 6)) ?? ""
                });
            }
        }
        finally
        {
            sqlite3_finalize(stmt);
        }
        return list;
    }

    public void InsertAccount(Account a)
    {
        Open();
        string sql = "INSERT INTO accounts (id, name, type, initial_balance, current_balance, currency, description) VALUES (?, ?, ?, ?, ?, ?, ?);";
        int rc = sqlite3_prepare16_v2(_db, sql, -1, out var stmt, IntPtr.Zero);
        if (rc != SQLITE_OK) throw new InvalidOperationException(GetLastErrorMessage());

        try
        {
            sqlite3_bind_text16(stmt, 1, a.Id, -1, IntPtr.Zero);
            sqlite3_bind_text16(stmt, 2, a.Name, -1, IntPtr.Zero);
            sqlite3_bind_int(stmt, 3, (int)a.Type);
            sqlite3_bind_double(stmt, 4, (double)a.InitialBalance);
            sqlite3_bind_double(stmt, 5, (double)a.CurrentBalance);
            sqlite3_bind_text16(stmt, 6, a.Currency, -1, IntPtr.Zero);
            sqlite3_bind_text16(stmt, 7, a.Description, -1, IntPtr.Zero);
            sqlite3_step(stmt);
        }
        finally
        {
            sqlite3_finalize(stmt);
        }
    }

    public void UpdateAccount(Account a)
    {
        Open();
        string sql = "UPDATE accounts SET name = ?, type = ?, description = ? WHERE id = ?;";
        int rc = sqlite3_prepare16_v2(_db, sql, -1, out var stmt, IntPtr.Zero);
        if (rc != SQLITE_OK) throw new InvalidOperationException(GetLastErrorMessage());

        try
        {
            sqlite3_bind_text16(stmt, 1, a.Name, -1, IntPtr.Zero);
            sqlite3_bind_int(stmt, 2, (int)a.Type);
            sqlite3_bind_text16(stmt, 3, a.Description, -1, IntPtr.Zero);
            sqlite3_bind_text16(stmt, 4, a.Id, -1, IntPtr.Zero);
            sqlite3_step(stmt);
        }
        finally
        {
            sqlite3_finalize(stmt);
        }
    }

    public bool DeleteAccount(string id, out string error)
    {
        Open();
        // Check if transactions exist
        var txs = GetTransactions().Where(t => t.SourceAccountId == id || t.DestinationAccountId == id).ToList();
        if (txs.Count > 0)
        {
            error = "Không thể xóa tài khoản/ví đang có giao dịch phát sinh. Hãy xóa các giao dịch liên quan trước.";
            return false;
        }

        string sql = "DELETE FROM accounts WHERE id = ?;";
        int rc = sqlite3_prepare16_v2(_db, sql, -1, out var stmt, IntPtr.Zero);
        if (rc != SQLITE_OK)
        {
            error = GetLastErrorMessage();
            return false;
        }

        try
        {
            sqlite3_bind_text16(stmt, 1, id, -1, IntPtr.Zero);
            sqlite3_step(stmt);
            error = string.Empty;
            return true;
        }
        finally
        {
            sqlite3_finalize(stmt);
        }
    }
    #endregion

    #region Categories CRUD
    public List<Category> GetCategories(TransactionType? type = null)
    {
        Open();
        var list = new List<Category>();
        string sql = type.HasValue
            ? $"SELECT id, name, type, description FROM categories WHERE type = {(int)type.Value} ORDER BY name;"
            : "SELECT id, name, type, description FROM categories ORDER BY name;";

        int rc = sqlite3_prepare16_v2(_db, sql, -1, out var stmt, IntPtr.Zero);
        if (rc != SQLITE_OK) return list;

        try
        {
            while (sqlite3_step(stmt) == SQLITE_ROW)
            {
                list.Add(new Category
                {
                    Id = Marshal.PtrToStringUni(sqlite3_column_text16(stmt, 0)) ?? "",
                    Name = Marshal.PtrToStringUni(sqlite3_column_text16(stmt, 1)) ?? "",
                    Type = (TransactionType)sqlite3_column_int(stmt, 2),
                    Description = Marshal.PtrToStringUni(sqlite3_column_text16(stmt, 3)) ?? ""
                });
            }
        }
        finally
        {
            sqlite3_finalize(stmt);
        }
        return list;
    }

    public void InsertCategory(Category c)
    {
        Open();
        string sql = "INSERT INTO categories (id, name, type, description) VALUES (?, ?, ?, ?);";
        int rc = sqlite3_prepare16_v2(_db, sql, -1, out var stmt, IntPtr.Zero);
        if (rc != SQLITE_OK) throw new InvalidOperationException(GetLastErrorMessage());

        try
        {
            sqlite3_bind_text16(stmt, 1, c.Id, -1, IntPtr.Zero);
            sqlite3_bind_text16(stmt, 2, c.Name, -1, IntPtr.Zero);
            sqlite3_bind_int(stmt, 3, (int)c.Type);
            sqlite3_bind_text16(stmt, 4, c.Description, -1, IntPtr.Zero);
            sqlite3_step(stmt);
        }
        finally
        {
            sqlite3_finalize(stmt);
        }
    }

    public void UpdateCategory(Category c)
    {
        Open();
        string sql = "UPDATE categories SET name = ?, type = ?, description = ? WHERE id = ?;";
        int rc = sqlite3_prepare16_v2(_db, sql, -1, out var stmt, IntPtr.Zero);
        if (rc != SQLITE_OK) throw new InvalidOperationException(GetLastErrorMessage());

        try
        {
            sqlite3_bind_text16(stmt, 1, c.Name, -1, IntPtr.Zero);
            sqlite3_bind_int(stmt, 2, (int)c.Type);
            sqlite3_bind_text16(stmt, 3, c.Description, -1, IntPtr.Zero);
            sqlite3_bind_text16(stmt, 4, c.Id, -1, IntPtr.Zero);
            sqlite3_step(stmt);
        }
        finally
        {
            sqlite3_finalize(stmt);
        }
    }

    public bool DeleteCategory(string id, out string error)
    {
        Open();
        var txs = GetTransactions().Where(t => t.CategoryId == id).ToList();
        if (txs.Count > 0)
        {
            error = "Không thể xóa danh mục đang có giao dịch liên kết.";
            return false;
        }

        string sql = "DELETE FROM categories WHERE id = ?;";
        int rc = sqlite3_prepare16_v2(_db, sql, -1, out var stmt, IntPtr.Zero);
        if (rc != SQLITE_OK)
        {
            error = GetLastErrorMessage();
            return false;
        }

        try
        {
            sqlite3_bind_text16(stmt, 1, id, -1, IntPtr.Zero);
            sqlite3_step(stmt);
            error = string.Empty;
            return true;
        }
        finally
        {
            sqlite3_finalize(stmt);
        }
    }
    #endregion

    #region Transactions CRUD
    public List<Transaction> GetTransactions()
    {
        Open();
        var list = new List<Transaction>();
        string sql = "SELECT id, tx_date, amount, type, category_id, source_account_id, dest_account_id, note, created_at FROM transactions ORDER BY tx_date DESC, created_at DESC;";
        int rc = sqlite3_prepare16_v2(_db, sql, -1, out var stmt, IntPtr.Zero);
        if (rc != SQLITE_OK) return list;

        try
        {
            while (sqlite3_step(stmt) == SQLITE_ROW)
            {
                string dateStr = Marshal.PtrToStringUni(sqlite3_column_text16(stmt, 1)) ?? "";
                string createdStr = Marshal.PtrToStringUni(sqlite3_column_text16(stmt, 8)) ?? "";

                list.Add(new Transaction
                {
                    Id = Marshal.PtrToStringUni(sqlite3_column_text16(stmt, 0)) ?? "",
                    TransactionDate = DateTime.TryParse(dateStr, out var d) ? d : DateTime.Today,
                    Amount = (decimal)sqlite3_column_double(stmt, 2),
                    Type = (TransactionType)sqlite3_column_int(stmt, 3),
                    CategoryId = Marshal.PtrToStringUni(sqlite3_column_text16(stmt, 4)),
                    SourceAccountId = Marshal.PtrToStringUni(sqlite3_column_text16(stmt, 5)),
                    DestinationAccountId = Marshal.PtrToStringUni(sqlite3_column_text16(stmt, 6)),
                    Note = Marshal.PtrToStringUni(sqlite3_column_text16(stmt, 7)) ?? "",
                    CreatedAt = DateTime.TryParse(createdStr, out var cd) ? cd : DateTime.Now
                });
            }
        }
        finally
        {
            sqlite3_finalize(stmt);
        }
        return list;
    }

    public void InsertTransaction(Transaction t)
    {
        Open();
        string sql = "INSERT INTO transactions (id, tx_date, amount, type, category_id, source_account_id, dest_account_id, note, created_at) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?);";
        int rc = sqlite3_prepare16_v2(_db, sql, -1, out var stmt, IntPtr.Zero);
        if (rc != SQLITE_OK) throw new InvalidOperationException(GetLastErrorMessage());

        try
        {
            sqlite3_bind_text16(stmt, 1, t.Id, -1, IntPtr.Zero);
            sqlite3_bind_text16(stmt, 2, t.TransactionDate.ToString("yyyy-MM-dd"), -1, IntPtr.Zero);
            sqlite3_bind_double(stmt, 3, (double)t.Amount);
            sqlite3_bind_int(stmt, 4, (int)t.Type);
            if (t.CategoryId != null) sqlite3_bind_text16(stmt, 5, t.CategoryId, -1, IntPtr.Zero);
            else sqlite3_bind_null(stmt, 5);

            if (t.SourceAccountId != null) sqlite3_bind_text16(stmt, 6, t.SourceAccountId, -1, IntPtr.Zero);
            else sqlite3_bind_null(stmt, 6);

            if (t.DestinationAccountId != null) sqlite3_bind_text16(stmt, 7, t.DestinationAccountId, -1, IntPtr.Zero);
            else sqlite3_bind_null(stmt, 7);

            sqlite3_bind_text16(stmt, 8, t.Note, -1, IntPtr.Zero);
            sqlite3_bind_text16(stmt, 9, t.CreatedAt.ToString("o"), -1, IntPtr.Zero);

            sqlite3_step(stmt);
        }
        finally
        {
            sqlite3_finalize(stmt);
        }

        RecalculateBalances();
    }

    public void UpdateTransaction(Transaction t)
    {
        Open();
        string sql = "UPDATE transactions SET tx_date = ?, amount = ?, type = ?, category_id = ?, source_account_id = ?, dest_account_id = ?, note = ? WHERE id = ?;";
        int rc = sqlite3_prepare16_v2(_db, sql, -1, out var stmt, IntPtr.Zero);
        if (rc != SQLITE_OK) throw new InvalidOperationException(GetLastErrorMessage());

        try
        {
            sqlite3_bind_text16(stmt, 1, t.TransactionDate.ToString("yyyy-MM-dd"), -1, IntPtr.Zero);
            sqlite3_bind_double(stmt, 2, (double)t.Amount);
            sqlite3_bind_int(stmt, 3, (int)t.Type);
            if (t.CategoryId != null) sqlite3_bind_text16(stmt, 4, t.CategoryId, -1, IntPtr.Zero);
            else sqlite3_bind_null(stmt, 4);

            if (t.SourceAccountId != null) sqlite3_bind_text16(stmt, 5, t.SourceAccountId, -1, IntPtr.Zero);
            else sqlite3_bind_null(stmt, 5);

            if (t.DestinationAccountId != null) sqlite3_bind_text16(stmt, 6, t.DestinationAccountId, -1, IntPtr.Zero);
            else sqlite3_bind_null(stmt, 6);

            sqlite3_bind_text16(stmt, 7, t.Note, -1, IntPtr.Zero);
            sqlite3_bind_text16(stmt, 8, t.Id, -1, IntPtr.Zero);

            sqlite3_step(stmt);
        }
        finally
        {
            sqlite3_finalize(stmt);
        }

        RecalculateBalances();
    }

    public void DeleteTransaction(string id)
    {
        Open();
        string sql = "DELETE FROM transactions WHERE id = ?;";
        int rc = sqlite3_prepare16_v2(_db, sql, -1, out var stmt, IntPtr.Zero);
        if (rc != SQLITE_OK) throw new InvalidOperationException(GetLastErrorMessage());

        try
        {
            sqlite3_bind_text16(stmt, 1, id, -1, IntPtr.Zero);
            sqlite3_step(stmt);
        }
        finally
        {
            sqlite3_finalize(stmt);
        }

        RecalculateBalances();
    }

    public void RecalculateBalances()
    {
        Open();
        var accounts = GetAccounts().ToDictionary(a => a.Id);
        foreach (var a in accounts.Values)
        {
            a.CurrentBalance = a.InitialBalance;
        }

        var txs = GetTransactions().OrderBy(t => t.TransactionDate).ThenBy(t => t.CreatedAt);
        foreach (var tx in txs)
        {
            if (tx.Type == TransactionType.Income && tx.SourceAccountId != null && accounts.TryGetValue(tx.SourceAccountId, out var accIn))
            {
                accIn.CurrentBalance += tx.Amount;
            }
            else if (tx.Type == TransactionType.Expense && tx.SourceAccountId != null && accounts.TryGetValue(tx.SourceAccountId, out var accOut))
            {
                accOut.CurrentBalance -= tx.Amount;
            }
            else if (tx.Type == TransactionType.Transfer)
            {
                if (tx.SourceAccountId != null && accounts.TryGetValue(tx.SourceAccountId, out var src))
                {
                    src.CurrentBalance -= tx.Amount;
                }
                if (tx.DestinationAccountId != null && accounts.TryGetValue(tx.DestinationAccountId, out var dst))
                {
                    dst.CurrentBalance += tx.Amount;
                }
            }
        }

        foreach (var a in accounts.Values)
        {
            string sql = "UPDATE accounts SET current_balance = ? WHERE id = ?;";
            int rc = sqlite3_prepare16_v2(_db, sql, -1, out var stmt, IntPtr.Zero);
            if (rc == SQLITE_OK)
            {
                sqlite3_bind_double(stmt, 1, (double)a.CurrentBalance);
                sqlite3_bind_text16(stmt, 2, a.Id, -1, IntPtr.Zero);
                sqlite3_step(stmt);
                sqlite3_finalize(stmt);
            }
        }
    }
    #endregion

    #region Backup & Restore
    public void BackupDatabase(string targetFilePath)
    {
        Close();
        try
        {
            if (File.Exists(_dbPath))
            {
                File.Copy(_dbPath, targetFilePath, true);
            }
            else
            {
                throw new FileNotFoundException("Tập tin cơ sở dữ liệu gốc không tồn tại để sao lưu.");
            }
        }
        finally
        {
            Open();
        }
    }

    public void RestoreDatabase(string sourceBackupPath)
    {
        if (!File.Exists(sourceBackupPath))
        {
            throw new FileNotFoundException("Tập tin sao lưu nguồn không tồn tại.");
        }

        Close();
        try
        {
            File.Copy(sourceBackupPath, _dbPath, true);
        }
        finally
        {
            Open();
            RecalculateBalances();
        }
    }
    #endregion

    #region Settings
    public string GetSetting(string key, string defaultValue = "")
    {
        Open();
        string sql = "SELECT value FROM settings WHERE key = ?;";
        int rc = sqlite3_prepare16_v2(_db, sql, -1, out var stmt, IntPtr.Zero);
        if (rc != SQLITE_OK) return defaultValue;

        try
        {
            sqlite3_bind_text16(stmt, 1, key, -1, IntPtr.Zero);
            if (sqlite3_step(stmt) == SQLITE_ROW)
            {
                return Marshal.PtrToStringUni(sqlite3_column_text16(stmt, 0)) ?? defaultValue;
            }
        }
        finally
        {
            sqlite3_finalize(stmt);
        }
        return defaultValue;
    }

    public void SetSetting(string key, string value)
    {
        Open();
        string sql = "INSERT INTO settings (key, value) VALUES (?, ?) ON CONFLICT(key) DO UPDATE SET value = excluded.value;";
        int rc = sqlite3_prepare16_v2(_db, sql, -1, out var stmt, IntPtr.Zero);
        if (rc != SQLITE_OK) throw new InvalidOperationException(GetLastErrorMessage());

        try
        {
            sqlite3_bind_text16(stmt, 1, key, -1, IntPtr.Zero);
            sqlite3_bind_text16(stmt, 2, value, -1, IntPtr.Zero);
            sqlite3_step(stmt);
        }
        finally
        {
            sqlite3_finalize(stmt);
        }
    }
    #endregion

    public void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }
}
