namespace PersonalFinance.Models;

public class Transaction
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime TransactionDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; } = TransactionType.Expense;
    public string? CategoryId { get; set; }
    public string? SourceAccountId { get; set; }
    public string? DestinationAccountId { get; set; }
    public string Note { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
