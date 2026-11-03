namespace PersonalFinance.Models;

public class FinancialSummary
{
    public decimal TotalAssets { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetSavings => TotalIncome - TotalExpense;
    public Dictionary<string, decimal> ExpenseByCategory { get; set; } = new();
    public Dictionary<string, decimal> BalanceByAccount { get; set; } = new();
}
