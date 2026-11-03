# Quản Lý Tài Chính Cá Nhân (WinForms SQLite UML)

Phần mềm Quản Lý Tài Chính Cá Nhân với giao diện cơ bản, chuẩn mực Windows Forms, không màu mè, không lệch tỷ lệ, hỗ trợ 100% Tiếng Việt có dấu, lưu trữ cơ sở dữ liệu SQLite và hỗ trợ tính năng Sao lưu / Phục hồi (Backup & Restore).

---

## 1. Điểm Nổi Bật

* **Giao diện chuẩn mực Windows Forms:** Sử dụng `TabControl` tiêu chuẩn và các `GroupBox` chia khối mạch lạc, không lệch khung hình, tương thích tốt với mọi độ phân giải.
* **Không màu mè, không gradient, không emoji:** Màu sắc cơ bản, nhã nhặn, chuyên nghiệp theo chuẩn ứng dụng quản trị văn phòng.
* **100% Tiếng Việt có dấu:** Toàn bộ bảng biểu, tiêu đề, nút bấm, thông báo đều sử dụng tiếng Việt tự nhiên, chuẩn mực.
* **Font Segoe UI chuẩn hóa:** Toàn bộ thành phần giao diện chỉ sử dụng font `Segoe UI`.
* **Cơ sở dữ liệu SQLite thực thụ:** Sử dụng tệp tin CSDL `personal_finance.db`, có thể mở và kiểm tra trực tiếp bằng *DB Browser for SQLite*.
* **Sao lưu & Phục hồi (Backup & Restore):** Sao lưu toàn bộ dữ liệu ra tệp `.db` riêng và khôi phục dữ liệu nhanh chóng chỉ với 1 cú nhấp chuột.
* **Tối giản mã nguồn & Không phụ thuộc bên ngoài:** Tận dụng SQLite engine native có sẵn trên Windows (`winsqlite3.dll`), biên dịch ngay lập tức mà không cần tải gói NuGet nặng nề.

---

## 2. Cấu Trúc Mã Nguồn

```
PersonalFinance/
├── Models/                     # Lớp Thực Thể (Domain Layer)
│   ├── Enums.cs                # TransactionType, AccountType
│   ├── Account.cs              # Thực thể Ví & Tài khoản ngân hàng
│   ├── Category.cs             # Thực thể Danh mục Thu/Chi
│   ├── Transaction.cs          # Thực thể Giao dịch tài chính
│   └── FinancialSummary.cs     # Đối tượng tổng hợp số liệu thống kê
├── Data/                       # Lớp Cơ Sở Dữ Liệu SQLite
│   └── SqliteDatabase.cs       # Động cơ SQLite native, CRUD, tính toán số dư, Backup/Restore
├── Services/                   # Lớp Nghiệp Vụ
│   ├── IFinanceService.cs      # Interface định nghĩa toàn bộ nghiệp vụ
│   └── FinanceService.cs       # Cầu nối giữa UI và CSDL, xử lý logic & sự kiện
├── UI/                         # Lớp Giao Diện Trực Quan (WinForms)
│   ├── Theme.cs                # Quản lý tập trung font Segoe UI và định dạng tiền tệ
│   ├── MainForm.cs             # Cửa sổ chính với TabControl (6 Tab chức năng)
│   └── Dialogs/                # Hộp thoại nhập liệu chuẩn Windows
│       ├── TransactionDialog.cs# Thêm / sửa giao dịch
│       ├── AccountDialog.cs    # Thêm / sửa ví tài khoản
│       ├── CategoryDialog.cs   # Thêm / sửa danh mục
│       └── TransferDialog.cs   # Chuyển tiền giữa các ví
├── Program.cs                  # Điểm khởi chạy ứng dụng
└── personal_finance.db         # Tệp cơ sở dữ liệu SQLite (tự động tạo)
```

---

## 3. Các Chức Năng Chính (6 Tab Chức Năng)

1. **Tổng quan:** Xem 4 thông số cốt lõi (Tổng tài sản khả dụng, Tổng thu nhập, Tổng chi tiêu, Tiết kiệm thuần), thao tác nhanh và bảng các giao dịch gần đây.
2. **Sổ giao dịch:** Quản lý toàn bộ giao dịch, hỗ trợ bộ lọc kết hợp theo ngày, loại thu/chi, ví và tìm kiếm theo ghi chú.
3. **Tài khoản & Ví:** Quản lý ví tiền mặt, tài khoản ngân hàng, thẻ tín dụng, ví điện tử, sổ tiết kiệm; hỗ trợ chuyển tiền nội bộ giữa các ví.
4. **Danh mục:** Quản lý danh mục chi tiêu và thu nhập.
5. **Báo cáo & Thống kê:** Báo cáo tỷ lệ tiết kiệm (%), bảng chi tiêu theo danh mục (%) và bảng phân bổ số dư trên từng ví.
6. **Sao lưu & Khôi phục:** Xem vị trí và dung lượng CSDL hiện tại, thực hiện sao lưu tệp `.db` và khôi phục khi cần.

---

## 4. Cách Chạy Ứng Dụng

Yêu cầu máy tính đã cài .NET SDK (hỗ trợ .NET 8 / 10).

```powershell
# Chạy ứng dụng trực tiếp từ Terminal
dotnet run
```
