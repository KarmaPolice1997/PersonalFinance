using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
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
    private ElementTheme DialogTheme => App.ThemeService?.CurrentTheme ?? ElementTheme.Default;
    private nint WindowHandle => App.MainWindowInstance != null
        ? WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance)
        : nint.Zero;

    public async Task ShowMessageAsync(string title, string message)
    {
        if (CurrentXamlRoot == null) return;

        var dlg = new ContentDialog
        {
            XamlRoot = CurrentXamlRoot,
            RequestedTheme = DialogTheme,
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
            RequestedTheme = DialogTheme,
            Title = title,
            Content = message,
            PrimaryButtonText = confirmText,
            CloseButtonText = cancelText,
            DefaultButton = ContentDialogButton.Primary
        };
        var res = await dlg.ShowAsync();
        return res == ContentDialogResult.Primary;
    }

    public Task<Transaction?> ShowAddTransactionDialogAsync() =>
        ShowTransactionDialogInternalAsync(null);

    public Task<Transaction?> ShowEditTransactionDialogAsync(Transaction existing) =>
        ShowTransactionDialogInternalAsync(existing);

    private async Task<Transaction?> ShowTransactionDialogInternalAsync(Transaction? existing)
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

        bool isEdit = existing != null;
        var stack = new StackPanel { Spacing = 12, Width = 360 };

        int initialTypeIndex = existing?.Type switch
        {
            TransactionType.Income => 1,
            TransactionType.Transfer => 2,
            _ => 0
        };

        var typeCombo = new ComboBox
        {
            Header = "Loại giao dịch",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            SelectedIndex = initialTypeIndex
        };
        typeCombo.Items.Add("Chi tiêu (Expense)");
        typeCombo.Items.Add("Thu nhập (Income)");
        typeCombo.Items.Add("Chuyển khoản (Transfer)");

        var datePicker = new CalendarDatePicker
        {
            Header = "Ngày giao dịch",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Date = existing != null ? new DateTimeOffset(existing.TransactionDate) : DateTimeOffset.Now
        };

        var amountBox = new TextBox
        {
            Header = "Số tiền (VND)",
            PlaceholderText = "Ví dụ: 50,000",
            Text = existing != null ? existing.Amount.ToString("N0") : string.Empty
        };

        var catCombo = new ComboBox { Header = "Danh mục", HorizontalAlignment = HorizontalAlignment.Stretch };
        void UpdateCategories(string? preferredCategoryId = null)
        {
            catCombo.Items.Clear();
            var list = typeCombo.SelectedIndex == 1 ? incomeCategories : expenseCategories;
            int selectedIdx = 0;
            for (int i = 0; i < list.Count; i++)
            {
                catCombo.Items.Add(list[i].Name);
                if (preferredCategoryId != null && list[i].Id == preferredCategoryId)
                {
                    selectedIdx = i;
                }
            }
            if (catCombo.Items.Count > 0) catCombo.SelectedIndex = selectedIdx;
            catCombo.Visibility = typeCombo.SelectedIndex == 2 ? Visibility.Collapsed : Visibility.Visible;
        }
        UpdateCategories(existing?.CategoryId);

        var accCombo = new ComboBox { Header = "Tài khoản nguồn", HorizontalAlignment = HorizontalAlignment.Stretch };
        int srcIndex = 0;
        for (int i = 0; i < accounts.Count; i++)
        {
            accCombo.Items.Add(accounts[i].Name);
            if (existing?.SourceAccountId == accounts[i].Id) srcIndex = i;
        }
        if (accCombo.Items.Count > 0) accCombo.SelectedIndex = srcIndex;

        var destAccCombo = new ComboBox
        {
            Header = "Tài khoản đích",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Visibility = initialTypeIndex == 2 ? Visibility.Visible : Visibility.Collapsed
        };
        int destIndex = accounts.Count > 1 ? 1 : 0;
        for (int i = 0; i < accounts.Count; i++)
        {
            destAccCombo.Items.Add(accounts[i].Name);
            if (existing?.DestinationAccountId == accounts[i].Id) destIndex = i;
        }
        if (destAccCombo.Items.Count > 0) destAccCombo.SelectedIndex = destIndex;

        typeCombo.SelectionChanged += (s, e) =>
        {
            UpdateCategories();
            destAccCombo.Visibility = typeCombo.SelectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
        };

        var noteBox = new TextBox
        {
            Header = "Ghi chú",
            PlaceholderText = "Ăn uống, lương, tiền điện...",
            Text = existing?.Note ?? string.Empty
        };

        stack.Children.Add(typeCombo);
        stack.Children.Add(datePicker);
        stack.Children.Add(amountBox);
        stack.Children.Add(catCombo);
        stack.Children.Add(accCombo);
        stack.Children.Add(destAccCombo);
        stack.Children.Add(noteBox);

        var dialog = new ContentDialog
        {
            XamlRoot = CurrentXamlRoot,
            RequestedTheme = DialogTheme,
            Title = isEdit ? "Sửa giao dịch" : "Thêm giao dịch mới",
            PrimaryButtonText = isEdit ? "Cập nhật" : "Lưu giao dịch",
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
        DateTime selectedDate = datePicker.Date?.DateTime ?? DateTime.Now;
        if (existing != null && selectedDate.Date == existing.TransactionDate.Date)
        {
            selectedDate = existing.TransactionDate;
        }
        else if (selectedDate.TimeOfDay == TimeSpan.Zero && selectedDate.Date == DateTime.Today)
        {
            selectedDate = DateTime.Now;
        }

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

                if (isEdit && existing != null)
                {
                    var updated = new Transaction
                    {
                        Id = existing.Id,
                        CreatedAt = existing.CreatedAt,
                        TransactionDate = selectedDate,
                        Amount = amount,
                        Type = TransactionType.Transfer,
                        CategoryId = null,
                        SourceAccountId = selSourceAcc.Id,
                        DestinationAccountId = selDestAcc.Id,
                        Note = string.IsNullOrWhiteSpace(noteBox.Text) ? "Chuyển tiền giữa các ví" : noteBox.Text.Trim()
                    };
                    _financeService.UpdateTransaction(updated);
                    return updated;
                }

                _financeService.TransferFunds(selSourceAcc.Id, selDestAcc.Id, amount, selectedDate, noteBox.Text.Trim());
                return null;
            }

            var txType = typeCombo.SelectedIndex == 1 ? TransactionType.Income : TransactionType.Expense;
            var catList = txType == TransactionType.Income ? incomeCategories : expenseCategories;
            var selCategory = catCombo.SelectedIndex >= 0 && catCombo.SelectedIndex < catList.Count
                ? catList[catCombo.SelectedIndex]
                : null;

            if (isEdit && existing != null)
            {
                var updated = new Transaction
                {
                    Id = existing.Id,
                    CreatedAt = existing.CreatedAt,
                    TransactionDate = selectedDate,
                    Amount = amount,
                    Type = txType,
                    CategoryId = selCategory?.Id,
                    SourceAccountId = selSourceAcc.Id,
                    DestinationAccountId = null,
                    Note = noteBox.Text.Trim()
                };
                _financeService.UpdateTransaction(updated);
                return updated;
            }

            var tx = new Transaction
            {
                TransactionDate = selectedDate,
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

    public Task<Account?> ShowAddAccountDialogAsync() =>
        ShowAccountDialogInternalAsync(null);

    public Task<Account?> ShowEditAccountDialogAsync(Account existing) =>
        ShowAccountDialogInternalAsync(existing);

    private async Task<Account?> ShowAccountDialogInternalAsync(Account? existing)
    {
        if (CurrentXamlRoot == null) return null;

        bool isEdit = existing != null;
        var stack = new StackPanel { Spacing = 12, Width = 340 };
        var nameBox = new TextBox
        {
            Header = "Tên tài khoản / Ví / Thẻ",
            PlaceholderText = "Ví dụ: MB Bank, Tiền mặt",
            Text = existing?.Name ?? string.Empty
        };

        var typeCombo = new ComboBox
        {
            Header = "Loại tài khoản",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            SelectedIndex = existing != null ? Math.Clamp((int)existing.Type, 0, 4) : 0
        };
        typeCombo.Items.Add("Tiền mặt (Cash)");
        typeCombo.Items.Add("Ngân hàng (Bank)");
        typeCombo.Items.Add("Thẻ tín dụng (Credit Card)");
        typeCombo.Items.Add("Ví điện tử (E-Wallet)");
        typeCombo.Items.Add("Sổ tiết kiệm (Savings)");

        var balanceBox = new TextBox
        {
            Header = "Số dư ban đầu (VND)",
            PlaceholderText = "0",
            Text = existing != null ? existing.InitialBalance.ToString("N0") : string.Empty
        };
        var descBox = new TextBox
        {
            Header = "Ghi chú",
            PlaceholderText = "Tài khoản chi tiêu chính",
            Text = existing?.Description ?? string.Empty
        };

        stack.Children.Add(nameBox);
        stack.Children.Add(typeCombo);
        stack.Children.Add(balanceBox);
        stack.Children.Add(descBox);

        var dlg = new ContentDialog
        {
            XamlRoot = CurrentXamlRoot,
            RequestedTheme = DialogTheme,
            Title = isEdit ? "Sửa tài khoản / ví" : "Thêm ví / thẻ mới",
            PrimaryButtonText = isEdit ? "Cập nhật" : "Tạo tài khoản",
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

        if (!string.IsNullOrWhiteSpace(balanceBox.Text) && !Formatters.TryParseCurrency(balanceBox.Text, out decimal initialBal))
        {
            await ShowMessageAsync("Lỗi dữ liệu", "Số dư ban đầu không hợp lệ.");
            return null;
        }
        else if (string.IsNullOrWhiteSpace(balanceBox.Text))
        {
            initialBal = 0m;
        }

        try
        {
            if (isEdit && existing != null)
            {
                var updated = new Account
                {
                    Id = existing.Id,
                    Name = nameBox.Text.Trim(),
                    Type = (AccountType)typeCombo.SelectedIndex,
                    InitialBalance = initialBal,
                    CurrentBalance = existing.CurrentBalance,
                    Currency = existing.Currency,
                    Description = descBox.Text.Trim(),
                    CreatedAt = existing.CreatedAt,
                    IsActive = existing.IsActive
                };
                _financeService.UpdateAccount(updated);
                return updated;
            }

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
            await ShowMessageAsync(isEdit ? "Lỗi cập nhật tài khoản" : "Lỗi tạo tài khoản", ex.Message);
            return null;
        }
    }

    public async Task ShowManageCategoriesDialogAsync()
    {
        if (CurrentXamlRoot == null) return;

        var rootStack = new StackPanel { Spacing = 12, Width = 420 };

        var nameBox = new TextBox
        {
            PlaceholderText = "Tên danh mục mới...",
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var typeCombo = new ComboBox { SelectedIndex = 0, Width = 120 };
        typeCombo.Items.Add("Chi tiêu");
        typeCombo.Items.Add("Thu nhập");

        var addBtn = new Button { Content = "Thêm" };

        var addGrid = new Grid { ColumnSpacing = 8 };
        addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(nameBox, 0);
        Grid.SetColumn(typeCombo, 1);
        Grid.SetColumn(addBtn, 2);
        addGrid.Children.Add(nameBox);
        addGrid.Children.Add(typeCombo);
        addGrid.Children.Add(addBtn);

        var errorText = new TextBlock
        {
            Foreground = Formatters.RedBrush,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Visibility = Visibility.Collapsed
        };

        var listStack = new StackPanel { Spacing = 6 };
        var scrollViewer = new ScrollViewer
        {
            MaxHeight = 280,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = listStack
        };

        void RebuildCategoryList()
        {
            listStack.Children.Clear();
            var categories = _financeService.GetAllCategories();
            foreach (var cat in categories)
            {
                var rowGrid = new Grid { ColumnSpacing = 8 };
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var titleBlock = new TextBlock
                {
                    Text = cat.Name,
                    FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };

                bool isIncome = cat.Type == TransactionType.Income;
                var badge = new Border
                {
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(8, 2, 8, 2),
                    Background = isIncome ? Formatters.MintBgBrush : Formatters.RedBgBrush,
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = isIncome ? "Thu nhập" : "Chi tiêu",
                        FontSize = 11,
                        Foreground = isIncome ? Formatters.MintBrush : Formatters.RedBrush
                    }
                };

                var delBtn = new Button
                {
                    Content = new FontIcon { Glyph = "\uE74D", FontSize = 12 },
                    Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(6)
                };
                ToolTipService.SetToolTip(delBtn, "Xóa danh mục");

                string catId = cat.Id;
                delBtn.Click += (_, _) =>
                {
                    errorText.Visibility = Visibility.Collapsed;
                    if (_financeService.DeleteCategory(catId, out string err))
                    {
                        RebuildCategoryList();
                    }
                    else
                    {
                        errorText.Text = err;
                        errorText.Visibility = Visibility.Visible;
                    }
                };

                Grid.SetColumn(titleBlock, 0);
                Grid.SetColumn(badge, 1);
                Grid.SetColumn(delBtn, 2);
                rowGrid.Children.Add(titleBlock);
                rowGrid.Children.Add(badge);
                rowGrid.Children.Add(delBtn);

                var rowBorder = new Border
                {
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(10, 6, 6, 6),
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(40, 140, 140, 160)),
                    Child = rowGrid
                };

                listStack.Children.Add(rowBorder);
            }
        }

        addBtn.Click += (_, _) =>
        {
            errorText.Visibility = Visibility.Collapsed;
            string name = nameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                errorText.Text = "Tên danh mục không được để trống.";
                errorText.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                var newCat = new Category
                {
                    Name = name,
                    Type = typeCombo.SelectedIndex == 1 ? TransactionType.Income : TransactionType.Expense
                };
                _financeService.CreateCategory(newCat);
                nameBox.Text = string.Empty;
                RebuildCategoryList();
            }
            catch (Exception ex)
            {
                errorText.Text = ex.Message;
                errorText.Visibility = Visibility.Visible;
            }
        };

        RebuildCategoryList();

        rootStack.Children.Add(addGrid);
        rootStack.Children.Add(errorText);
        rootStack.Children.Add(scrollViewer);

        var dlg = new ContentDialog
        {
            XamlRoot = CurrentXamlRoot,
            RequestedTheme = DialogTheme,
            Title = "Quản lý danh mục Thu / Chi",
            CloseButtonText = "Đóng",
            Content = rootStack
        };

        await dlg.ShowAsync();
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
