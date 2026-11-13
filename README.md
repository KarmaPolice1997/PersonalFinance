# Atom Finance — Quản Lý Tài Chính Cá Nhân (WinUI 3 & Native SQLite)

Ứng dụng Quản Lý Tài Chính Cá Nhân viết bằng **C# / WinUI 3 (Windows App SDK)** kết hợp kiến trúc **MVVM (`CommunityToolkit.Mvvm`)** và cơ sở dữ liệu **SQLite native (`winsqlite3.dll`)** không cần cài đặt phụ thuộc bên ngoài.

---

## 1. Điểm Nổi Bật

* **Giao diện WinUI 3 hiện đại (Mica Backdrop):** Hỗ trợ 3 chế độ hiển thị **Theo hệ thống Windows / Sáng (Light) / Tối (Dark)**, chuyển trang tức thì không giật lag và đồng bộ theme trên cả `ContentDialog`.
* **Biểu đồ tương tác 7 ngày & Đồng hồ ngân sách động:** Biểu đồ đường cong Cubic Bézier kết hợp cột chi tiêu 7 ngày gần nhất, hiển thị chi tiết khi rê chuột và đồng hồ bán nguyệt phản ánh tỷ lệ chi tiêu so với hạn mức tháng.
* **Bố cục chia khung linh hoạt (`PaneSplitter`):** Kéo thả để thay đổi kích thước bảng phải, thẻ số dư và khu vực biểu đồ; double-click để đặt lại và tự động lưu cấu hình vào SQLite.
* **Quản lý đầy đủ Thu / Chi / Chuyển khoản / Ví / Danh mục:**
  * Thêm, sửa, xóa giao dịch kèm chọn ngày (`CalendarDatePicker`).
  * Lọc giao dịch theo loại (Tất cả / Chi tiêu / Thu nhập / Chuyển khoản), theo ví, theo danh mục và tìm kiếm theo từ khóa.
  * Thêm, sửa, xóa ví / tài khoản và quản lý danh mục Thu / Chi.
  * Tùy chỉnh hạn mức chi tiêu tháng trong Cài đặt.
* **Động cơ SQLite Native (`winsqlite3.dll`):** Sử dụng các câu lệnh chuẩn bị sẵn (prepared statements), tự động tính lại số dư theo trình tự thời gian trong transaction, lưu dữ liệu an toàn tại `%LocalAppData%\AtomFinance\personal_finance.db`.
* **Sao lưu & Phục hồi có kiểm tra toàn vẹn:** Hỗ trợ sao lưu ra tệp `.db` và kiểm tra định dạng SQLite + cấu trúc bảng trước khi phục hồi.

---

## 2. Cấu Trúc Mã Nguồn

```text
PersonalFinance/
├── App.xaml / App.xaml.cs          # Khởi tạo ứng dụng, services và MainWindow
├── MainWindow.xaml(.cs)            # Shell chính với thanh điều hướng bên trái & Frame
├── Controls/
│   ├── EqualColumnsPanel.cs        # Panel chia cột đều cho biểu đồ 7 ngày
│   ├── PaneSplitter.cs             # Thanh chia khung kéo thả + lưu kích thước
│   └── WrapPanel.cs                # Panel tự động xuống dòng cho danh sách thẻ/ví
├── Data/
│   └── SqliteDatabase.cs           # P/Invoke winsqlite3.dll, CRUD, tính số dư, Backup/Restore
├── Helpers/
│   └── Formatters.cs               # Định dạng tiền tệ VND/USD, ngày tương đối, phân tích số tiền
├── Models/
│   ├── Account.cs                  # Thực thể Ví & Tài khoản
│   ├── Category.cs                 # Thực thể Danh mục Thu/Chi
│   ├── Transaction.cs              # Thực thể Giao dịch tài chính
│   ├── FinancialSummary.cs         # Tổng hợp tài sản, thu nhập, chi tiêu
│   ├── DisplayModels.cs            # Model hiển thị UI (TransactionItem, AccountItem...)
│   └── Enums.cs                    # TransactionType, AccountType
├── Services/
│   ├── IFinanceService.cs          # Interface nghiệp vụ tài chính
│   ├── FinanceService.cs           # Xử lý nghiệp vụ, lọc, thống kê & sự kiện DataChanged
│   ├── IDialogService.cs           # Interface hộp thoại & FilePicker
│   ├── DialogService.cs            # ContentDialog (Thêm/Sửa giao dịch, ví, danh mục) & Picker
│   └── ThemeService.cs             # Quản lý và lưu trữ chế độ giao diện
├── Themes/
│   └── Colors.xaml                 # Bảng màu Dark (Default) & Light
├── ViewModels/
│   ├── ViewModelBase.cs            # Tự động đồng bộ dữ liệu qua DataChanged & DispatcherQueue
│   ├── ShellViewModel.cs           # Điều hướng trang & tìm kiếm chuyển tiếp
│   ├── DashboardViewModel.cs       # Số liệu tổng quan, biểu đồ 7 ngày, thông báo & bố cục
│   ├── TransactionsViewModel.cs    # Sổ giao dịch, bộ lọc & CRUD giao dịch
│   ├── AccountsViewModel.cs        # Quản lý ví / thẻ & tổng tài sản
│   └── SettingsViewModel.cs        # Giao diện, hạn mức tháng, danh mục & sao lưu/phục hồi
└── Views/
    ├── DashboardPage.xaml(.cs)     # Trang Tổng quan
    ├── TransactionsPage.xaml(.cs)  # Trang Sổ giao dịch (ListView ảo hóa)
    ├── AccountsPage.xaml(.cs)      # Trang Ví & Tài khoản
    └── SettingsPage.xaml(.cs)      # Trang Cài đặt & Dữ liệu
```

---

## 3. Yêu Cầu & Cách Chạy Ứng Dụng

* **Hệ điều hành:** Windows 10 (1809+) / Windows 11
* **SDK:** .NET SDK hỗ trợ `net11.0-windows10.0.26100.0` và Windows App SDK

```powershell
# Biên dịch dự án
dotnet build PersonalFinance.csproj

# Chạy ứng dụng
dotnet run --project PersonalFinance.csproj
```
