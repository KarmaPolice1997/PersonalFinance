using PersonalFinance.Models;

namespace PersonalFinance.Services;

public interface IDataContext
{
    IRepository<Account> Accounts { get; }
    IRepository<Category> Categories { get; }
    IRepository<Transaction> Transactions { get; }
    void SaveChanges();
    void Reload();
}
