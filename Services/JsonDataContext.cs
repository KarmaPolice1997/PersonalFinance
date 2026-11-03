using System.Text.Encodings.Web;
using System.Text.Json;
using PersonalFinance.Models;

namespace PersonalFinance.Services;

public class AppDatabase
{
    public List<Account> Accounts { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<Transaction> Transactions { get; set; } = new();
}

public class JsonDataContext : IDataContext
{
    private readonly string _filePath;
    private readonly InMemoryRepository<Account> _accountsRepo = new(a => a.Id);
    private readonly InMemoryRepository<Category> _categoriesRepo = new(c => c.Id);
    private readonly InMemoryRepository<Transaction> _transactionsRepo = new(t => t.Id);

    public IRepository<Account> Accounts => _accountsRepo;
    public IRepository<Category> Categories => _categoriesRepo;
    public IRepository<Transaction> Transactions => _transactionsRepo;

    public JsonDataContext(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "finance_data.json");
        Reload();
    }

    public void Reload()
    {
        if (File.Exists(_filePath))
        {
            try
            {
                var json = File.ReadAllText(_filePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var db = JsonSerializer.Deserialize<AppDatabase>(json, options);
                if (db != null)
                {
                    _accountsRepo.Load(db.Accounts ?? new List<Account>());
                    _categoriesRepo.Load(db.Categories ?? new List<Category>());
                    _transactionsRepo.Load(db.Transactions ?? new List<Transaction>());
                    return;
                }
            }
            catch
            {
                // Fallback to seed data if corrupted
            }
        }

        SeedInitialData();
        SaveChanges();
    }

    public void SaveChanges()
    {
        var db = new AppDatabase
        {
            Accounts = _accountsRepo.GetAll().ToList(),
            Categories = _categoriesRepo.GetAll().ToList(),
            Transactions = _transactionsRepo.GetAll().ToList()
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var json = JsonSerializer.Serialize(db, options);
        File.WriteAllText(_filePath, json);
    }

    private void SeedInitialData()
    {
        _accountsRepo.Clear();
        _categoriesRepo.Clear();
        _transactionsRepo.Clear();

        // 1. Seed Accounts
        var accCash = new Account
        {
            Id = "acc-cash",
            Name = "Vi tien mat",
            Type = AccountType.Cash,
            InitialBalance = 5_000_000,
            CurrentBalance = 5_000_000,
            Description = "Tien mat chi tieu hang ngay"
        };
        var accBank = new Account
        {
            Id = "acc-bank",
            Name = "Tai khoan Vietcombank",
            Type = AccountType.Bank,
            InitialBalance = 35_000_000,
            CurrentBalance = 35_000_000,
            Description = "Tai khoan nhan luong va thanh toan"
        };
        var accMomo = new Account
        {
            Id = "acc-momo",
            Name = "Vi dien tu MoMo",
            Type = AccountType.EWallet,
            InitialBalance = 2_000_000,
            CurrentBalance = 2_000_000,
            Description = "Thanh toan hoa don, mua sắm online"
        };
        var accSavings = new Account
        {
            Id = "acc-savings",
            Name = "Tiet kiem BIDV",
            Type = AccountType.Savings,
            InitialBalance = 80_000_000,
            CurrentBalance = 80_000_000,
            Description = "So tiet kiem ky han 6 thang"
        };

        _accountsRepo.Add(accCash);
        _accountsRepo.Add(accBank);
        _accountsRepo.Add(accMomo);
        _accountsRepo.Add(accSavings);

        // 2. Seed Categories
        var catFood = new Category { Id = "cat-food", Name = "An uong & Ca phe", Type = TransactionType.Expense };
        var catRent = new Category { Id = "cat-rent", Name = "Nha o & Dien nuoc", Type = TransactionType.Expense };
        var catTransport = new Category { Id = "cat-trans", Name = "Xang xe & Di lai", Type = TransactionType.Expense };
        var catShopping = new Category { Id = "cat-shop", Name = "Mua sam ca nhan", Type = TransactionType.Expense };
        var catEnt = new Category { Id = "cat-ent", Name = "Giai tri & Xem phim", Type = TransactionType.Expense };
        var catHealth = new Category { Id = "cat-health", Name = "Y te & Suc khoe", Type = TransactionType.Expense };

        var catSalary = new Category { Id = "cat-sal", Name = "Luong hang thang", Type = TransactionType.Income };
        var catBonus = new Category { Id = "cat-bonus", Name = "Thuong & Phu cap", Type = TransactionType.Income };
        var catInvest = new Category { Id = "cat-invest", Name = "Lai tiet kiem & Dau tu", Type = TransactionType.Income };

        _categoriesRepo.Add(catFood);
        _categoriesRepo.Add(catRent);
        _categoriesRepo.Add(catTransport);
        _categoriesRepo.Add(catShopping);
        _categoriesRepo.Add(catEnt);
        _categoriesRepo.Add(catHealth);
        _categoriesRepo.Add(catSalary);
        _categoriesRepo.Add(catBonus);
        _categoriesRepo.Add(catInvest);

        // 3. Seed Transactions (over past 10 days)
        var today = DateTime.Today;

        var tx1 = new Transaction
        {
            Id = Guid.NewGuid().ToString("N"),
            TransactionDate = today.AddDays(-6),
            Amount = 25_000_000,
            Type = TransactionType.Income,
            CategoryId = catSalary.Id,
            SourceAccountId = accBank.Id,
            Note = "Nhan luong thang qua chuyen khoan"
        };
        var tx2 = new Transaction
        {
            Id = Guid.NewGuid().ToString("N"),
            TransactionDate = today.AddDays(-5),
            Amount = 5_000_000,
            Type = TransactionType.Expense,
            CategoryId = catRent.Id,
            SourceAccountId = accBank.Id,
            Note = "Thanh toan tien nha thang"
        };
        var tx3 = new Transaction
        {
            Id = Guid.NewGuid().ToString("N"),
            TransactionDate = today.AddDays(-4),
            Amount = 1_500_000,
            Type = TransactionType.Transfer,
            SourceAccountId = accBank.Id,
            DestinationAccountId = accCash.Id,
            Note = "Rut tien ATM chi tieu tien mat"
        };
        var tx4 = new Transaction
        {
            Id = Guid.NewGuid().ToString("N"),
            TransactionDate = today.AddDays(-3),
            Amount = 350_000,
            Type = TransactionType.Expense,
            CategoryId = catFood.Id,
            SourceAccountId = accCash.Id,
            Note = "An trua va ca phe cung ban be"
        };
        var tx5 = new Transaction
        {
            Id = Guid.NewGuid().ToString("N"),
            TransactionDate = today.AddDays(-2),
            Amount = 1_200_000,
            Type = TransactionType.Expense,
            CategoryId = catShopping.Id,
            SourceAccountId = accMomo.Id,
            Note = "Mua giay the thao moi"
        };
        var tx6 = new Transaction
        {
            Id = Guid.NewGuid().ToString("N"),
            TransactionDate = today.AddDays(-1),
            Amount = 150_000,
            Type = TransactionType.Expense,
            CategoryId = catTransport.Id,
            SourceAccountId = accCash.Id,
            Note = "Do xang xe may"
        };

        _transactionsRepo.Add(tx1);
        _transactionsRepo.Add(tx2);
        _transactionsRepo.Add(tx3);
        _transactionsRepo.Add(tx4);
        _transactionsRepo.Add(tx5);
        _transactionsRepo.Add(tx6);

        // Recalculate balances
        RecalculateBalances();
    }

    private void RecalculateBalances()
    {
        var accounts = _accountsRepo.GetAll().ToDictionary(a => a.Id);
        foreach (var acc in accounts.Values)
        {
            acc.CurrentBalance = acc.InitialBalance;
        }

        foreach (var tx in _transactionsRepo.GetAll().OrderBy(t => t.TransactionDate))
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
