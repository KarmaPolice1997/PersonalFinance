using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AtomFinance.WinUI.Helpers;
using PersonalFinance.Models;
using PersonalFinance.Services;

namespace AtomFinance.WinUI.Services;

public class DialogService : IDialogService
{
    private readonly IFinanceService _financeService;

    public DialogService(IFinanceService financeService)
    {
        _financeService = financeService;
    }

    private XamlRoot? CurrentXamlRoot => App.MainWindowInstance?.Content?.XamlRoot;
    private nint WindowHandle => App.MainWindowInstance != null
        ? WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance)
        : nint.Zero;

    public async Task ShowMessageAsync(string title, string message)
    {
        if (CurrentXamlRoot == null) return;

        var dlg = new ContentDialog
        {
            XamlRoot = CurrentXamlRoot,
            Title = title,
            Content = message,
            CloseButtonText = "Đóng"
        };
        await dlg.ShowAsync();
    }

    public async Task<bool> ShowConfirmAsync(string title, string message, string confirmText = "Đồng ý", string cancelText = "Hủy")
    {
        if (CurrentXamlRoot == null) return false;

        var dlg = new ContentDialog
        {
            XamlRoot = CurrentXamlRoot,
            Title = title,
            Content = message,
            PrimaryButtonText = confirmText,
            CloseButtonText = cancelText,
            DefaultButton = ContentDialogButton.Primary
        };
        var res = await dlg.ShowAsync();
        return res == ContentDialogResult.Primary;
    }

    public async Task<Transaction?> ShowAddTransactionDialogAsync()
    {
        if (CurrentXamlRoot == null) return null;

        var accounts = _financeService.GetAllAccounts();
        var expenseCategories = _financeService.GetAllCategories(TransactionType.Expense);
        var incomeCategories = _financeService.GetAllCategories(TransactionType.Income);

        if (accounts.Count == 0)
        {
            await ShowMessageAsync("Chưa có tài khoản", "Vui lòng tạo ít nhất một tài khoản / thẻ trước khi ghi nhận giao dịch.");
            return null;
        }

        var stack = new StackPanel { Spacing = 12, Width = 340 };

        var typeCombo = new ComboBox { Header = "Loại giao dịch", HorizontalAlignment = HorizontalAlignment.Stretch, SelectedIndex = 0 };
        typeCombo.Items.Add("Chi tiêu (Expense)");
        typeCombo.Items.Add("Thu nhập (Income)");
        typeCombo.Items.Add("Chuyển khoản (Transfer)");

        var amountBox = new TextBox { Header = "Số tiền (VND)", PlaceholderText = "Ví dụ: 50,000" };

        var catCombo = new ComboBox { Header = "Danh mục", HorizontalAlignment = HorizontalAlignment.Stretch };
        void UpdateCategories()
        {
            catCombo.Items.Clear();
            var list = typeCombo.SelectedIndex == 1 ? incomeCategories : expenseCategories;
            foreach (var c in list) catCombo.Items.Add(c.Name);
            if (catCombo.Items.Count > 0) catCombo.SelectedIndex = 0;
            catCombo.Visibility = typeCombo.SelectedIndex == 2 ? Visibility.Collapsed : Visibility.Visible;
        }
        UpdateCategories();
        typeCombo.SelectionChanged += (s, e) => UpdateCategories();

        var accCombo = new ComboBox { Header = "Tài khoản nguồn", HorizontalAlignment = HorizontalAlignment.Stretch };
        foreach (var a in accounts) accCombo.Items.Add(a.Name);
        if (accCombo.Items.Count > 0) accCombo.SelectedIndex = 0;

        var destAccCombo = new ComboBox { Header = "Tài khoản đích", HorizontalAlignment = HorizontalAlignment.Stretch, Visibility = Visibility.Collapsed };
        foreach (var a in accounts) destAccCombo.Items.Add(a.Name);
        if (destAccCombo.Items.Count > 1) destAccCombo.SelectedIndex = 1;
        else if (destAccCombo.Items.Count > 0) destAccCombo.SelectedIndex = 0;

        typeCombo.SelectionChanged += (s, e) =>
        {
            destAccCombo.Visibility = typeCombo.SelectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
        };

        var noteBox = new TextBox { Header = "Ghi chú", PlaceholderText = "Ăn uống, lương, tiền điện..." };

        stack.Children.Add(typeCombo);
        stack.Children.Add(amountBox);
        stack.Children.Add(catCombo);
        stack.Children.Add(accCombo);
        stack.Children.Add(destAccCombo);
        stack.Children.Add(noteBox);

        var dialog = new ContentDialog
        {
            XamlRoot = CurrentXamlRoot,
            Title = "Thêm giao dịch mới",
            PrimaryButtonText = "Lưu giao dịch",
            CloseButtonText = "Hủy",
            DefaultButton = ContentDialogButton.Primary,
            Content = stack
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return null;

        if (!Formatters.TryParseCurrency(amountBox.Text, out decimal amount) || amount <= 0)
        {
            await ShowMessageAsync("Lỗi dữ liệu", "Số tiền giao dịch không hợp lệ (phải lớn hơn 0).");
            return null;
        }

        if (accCombo.SelectedIndex < 0 || accCombo.SelectedIndex >= accounts.Count)
        {
            await ShowMessageAsync("Lỗi dữ liệu", "Vui lòng chọn tài khoản nguồn.");
            return null;
        }

        var selSourceAcc = accounts[accCombo.SelectedIndex];

        try
        {
            if (typeCombo.SelectedIndex == 2) // Transfer
            {
                if (accounts.Count < 2)
                {
                    await ShowMessageAsync("Không thể chuyển khoản", "Cần ít nhất 2 tài khoản khác nhau để thực hiện chuyển khoản.");
                    return null;
                }

                if (destAccCombo.SelectedIndex < 0 || destAccCombo.SelectedIndex >= accounts.Count)
                {
                    await ShowMessageAsync("Lỗi dữ liệu", "Vui lòng chọn tài khoản đích.");
                    return null;
                }

                var selDestAcc = accounts[destAccCombo.SelectedIndex];
                if (selSourceAcc.Id == selDestAcc.Id)
                {
                    await ShowMessageAsync("Không hợp lệ", "Tài khoản nguồn và tài khoản đích phải khác nhau.");
                    return null;
                }

                _financeService.TransferFunds(selSourceAcc.Id, selDestAcc.Id, amount, DateTime.Now, noteBox.Text.Trim());
                return null;
            }

            var txType = typeCombo.SelectedIndex == 1 ? TransactionType.Income : TransactionType.Expense;
            var catList = txType == TransactionType.Income ? incomeCategories : expenseCategories;
            var selCategory = catCombo.SelectedIndex >= 0 && catCombo.SelectedIndex < catList.Count
                ? catList[catCombo.SelectedIndex]
                : null;

            var tx = new Transaction
            {
                TransactionDate = DateTime.Now,
                Amount = amount,
                Type = txType,
                CategoryId = selCategory?.Id,
                SourceAccountId = selSourceAcc.Id,
                Note = noteBox.Text.Trim()
            };

            _financeService.AddTransaction(tx);
            return tx;
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Lỗi lưu giao dịch", ex.Message);
            return null;
        }
    }

    public async Task<Account?> ShowAddAccountDialogAsync()
    {
        if (CurrentXamlRoot == null) return null;

        var stack = new StackPanel { Spacing = 12, Width = 320 };
        var nameBox = new TextBox { Header = "Tên tài khoản / Ví / Thẻ", PlaceholderText = "Ví dụ: MB Bank, Tiền mặt" };
        var typeCombo = new ComboBox { Header = "Loại tài khoản", HorizontalAlignment = HorizontalAlignment.Stretch, SelectedIndex = 0 };
        typeCombo.Items.Add("Tiền mặt (Cash)");
        typeCombo.Items.Add("Ngân hàng (Bank)");
        typeCombo.Items.Add("Thẻ tín dụng (Credit Card)");
        typeCombo.Items.Add("Ví điện tử (E-Wallet)");
        typeCombo.Items.Add("Sổ tiết kiệm (Savings)");

        var balanceBox = new TextBox { Header = "Số dư ban đầu (VND)", PlaceholderText = "0" };
        var descBox = new TextBox { Header = "Ghi chú", PlaceholderText = "Tài khoản chi tiêu chính" };

        stack.Children.Add(nameBox);
        stack.Children.Add(typeCombo);
        stack.Children.Add(balanceBox);
        stack.Children.Add(descBox);

        var dlg = new ContentDialog
        {
            XamlRoot = CurrentXamlRoot,
            Title = "Thêm ví / thẻ mới",
            PrimaryButtonText = "Tạo tài khoản",
            CloseButtonText = "Hủy",
            DefaultButton = ContentDialogButton.Primary,
            Content = stack
        };

        var result = await dlg.ShowAsync();
        if (result != ContentDialogResult.Primary)
            return null;

        if (string.IsNullOrWhiteSpace(nameBox.Text))
        {
            await ShowMessageAsync("Lỗi dữ liệu", "Tên tài khoản không được để trống.");
            return null;
        }

        Formatters.TryParseCurrency(balanceBox.Text, out decimal initialBal);

        try
        {
            var newAcc = new Account
            {
                Name = nameBox.Text.Trim(),
                Type = (AccountType)typeCombo.SelectedIndex,
                InitialBalance = initialBal,
                CurrentBalance = initialBal,
                Description = descBox.Text.Trim()
            };

            _financeService.CreateAccount(newAcc);
            return newAcc;
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Lỗi tạo tài khoản", ex.Message);
            return null;
        }
    }

    public async Task<string?> PickSaveDatabaseFileAsync()
    {
        try
        {
            var savePicker = new Windows.Storage.Pickers.FileSavePicker();
            savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("SQLite Database", new List<string>() { ".db" });
            savePicker.SuggestedFileName = $"PersonalFinance_Backup_{DateTime.Now:yyyyMMdd_HHmmss}";

            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, WindowHandle);
            var file = await savePicker.PickSaveFileAsync();
            return file?.Path;
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> PickOpenDatabaseFileAsync()
    {
        try
        {
            var openPicker = new Windows.Storage.Pickers.FileOpenPicker();
            openPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            openPicker.FileTypeFilter.Add(".db");

            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, WindowHandle);
            var file = await openPicker.PickSingleFileAsync();
            return file?.Path;
        }
        catch
        {
            return null;
        }
    }
}
