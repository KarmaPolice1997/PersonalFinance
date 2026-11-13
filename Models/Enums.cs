namespace PersonalFinance.Models;

public enum TransactionType
{
    Expense = 0, // Chi tieu
    Income = 1,  // Thu nhap
    Transfer = 2 // Chuyen khoan
}

public enum AccountType
{
    Cash = 0,        // Tien mat
    Bank = 1,        // Ngan hang
    CreditCard = 2,  // The tin dung
    EWallet = 3,     // Vi dien tu
    Savings = 4      // Tiet kiem
}
