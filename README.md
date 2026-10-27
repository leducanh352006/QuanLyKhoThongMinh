# Quản lý kho thông minh

Ứng dụng web quản lý kho cho cửa hàng bán lẻ có một hoặc nhiều kho: nhập kho, xuất kho, kiểm tra hàng hóa, điều chuyển giữa các kho, kèm bốn gợi ý AI (G1 đến G4). Đồ án nhóm 5 người; báo cáo mô tả đầy đủ nghiệp vụ và cơ sở dữ liệu.

Công nghệ: ASP.NET Core MVC trên .NET 10, Entity Framework Core, SQL Server, HTML/CSS/JavaScript (báo cáo mục 1.4).

Repo này đang ở mức **khung dự án** (việc T4-1, issue #41): chạy được, chưa có nghiệp vụ. Mỗi thành viên làm việc của mình trong Area riêng.

## Chạy lần đầu

Cần cài: [.NET 10 SDK](https://dotnet.microsoft.com/download), Git, và SQL Server (LocalDB đi kèm Visual Studio, hoặc SQL Server Express/Developer).

1. Tải mã nguồn

   ```
   git clone https://github.com/leducanh352006/QuanLyKhoThongMinh.git
   cd QuanLyKhoThongMinh
   ```

2. Tin cậy chứng chỉ HTTPS của môi trường phát triển (làm một lần trên máy)

   ```
   dotnet dev-certs https --trust
   ```

3. Đặt chuỗi kết nối SQL Server. Chuỗi kết nối **không được commit** lên git; chọn một trong hai cách:

   - User Secrets (khuyên dùng):

     ```
     cd src/QuanLyKho.Web
     dotnet user-secrets set "ConnectionStrings:KhoDb" "Server=(localdb)\MSSQLLocalDB;Database=QuanLyKho;Trusted_Connection=True;TrustServerCertificate=True"
     cd ../..
     ```

   - Hoặc sao `src/QuanLyKho.Web/appsettings.Development.example.json` thành `appsettings.Development.json` (file này đã nằm trong `.gitignore`) rồi sửa chuỗi kết nối.

   Dùng SQL Server Express thì đổi `Server=(localdb)\MSSQLLocalDB` thành `Server=.\SQLEXPRESS` (cùng máy) hoặc `Server=TEN-MAY\SQLEXPRESS`; đăng nhập `sa` thì thay `Trusted_Connection=True` bằng `User Id=sa;Password=...`. Database `QuanLyKho` phải được tạo trước (`CREATE DATABASE QuanLyKho;` trong SSMS). Có thể bỏ qua bước này lúc đầu: ứng dụng vẫn chạy và trang chủ báo "Chưa cấu hình" ở mục Cơ sở dữ liệu.

4. Chạy

   ```
   dotnet run --project src/QuanLyKho.Web --launch-profile https
   ```

   Mở https://localhost:7141 (hoặc địa chỉ in ra trong cửa sổ lệnh). Chưa đăng nhập sẽ được chuyển tới trang đăng nhập. Sau khi đăng nhập, trang chủ hiện vai trò, menu theo vai trò và trạng thái kết nối SQL Server.

5. Đặt mật khẩu cho 4 tài khoản mẫu (không nằm trong repo; mật khẩu cần ít nhất 8 ký tự, có chữ và số), rồi đăng nhập:

   ```
   cd src/QuanLyKho.Web
   dotnet user-secrets set "Auth:SeedPassword" "<mật khẩu bạn chọn>"
   cd ../..
   ```

   Tài khoản mẫu: `quantri` (Quản trị viên), `quanlykho` (Quản lý kho), `thukho` (Thủ kho), `nhanvienbh` (Nhân viên bán hàng), cùng một mật khẩu ở trên. Mỗi vai trò thấy menu khác nhau. Chưa có SQL Server thì ở môi trường Development ứng dụng dùng kho người dùng trong bộ nhớ nạp từ `SeedData` (mất khi tắt ứng dụng). Chi tiết đăng nhập, phân quyền và cách dùng `[CanQuyen]` trong `src/QuanLyKho.Web/Areas/HeThong/README.md`.

## Cấu trúc thư mục

```
QuanLyKhoThongMinh.sln
src/QuanLyKho.Web/
  Areas/                  Mỗi nhóm một Area, trong đó có Controllers, Views, Services, Models
    HeThong/              Nhóm 1 (TV4): hệ thống nền, kho, vị trí, tồn kho, khung gợi ý AI
    NhapKho/              Nhóm 2 (TV1)
    XuatKho/              Nhóm 3 (TV2)
    HangHoa/              Nhóm 4 (TV3): danh mục hàng hóa và kiểm tra hàng hóa
    DieuChuyen/           Nhóm 5 (TV5)
  Contracts/              Interface dịch vụ dùng chung giữa các nhóm + bản giả (Stubs)
  SeedData/               Dữ liệu mẫu chung (2 kho, ô, sản phẩm, biến thể, lô, 4 người dùng)
  Data/                   KhoDbContext và khởi tạo CSDL
  Infrastructure/         Menu theo vai trò, người dùng hiện tại, trạng thái CSDL
  Views/ wwwroot/         Layout chung, trang chủ, CSS, thành phần quét mã
tests/QuanLyKho.Tests/    Kiểm thử (xUnit)
```

## Quy tắc gộp code (báo cáo mục 1.3)

- Controller, View, Service, Model của nhóm nằm trong Area của nhóm. **Không sửa file trong Area của người khác.**
- Mỗi Area có một file `<Area>Module.cs` để đăng ký dịch vụ và menu của Area. Chỉ người phụ trách sửa file đó, nên không ai phải sửa `Program.cs`.
- `main` chỉ nhận code qua pull request và phải có người khác duyệt. Mỗi đầu việc một nhánh `feature/tv<số>-<tên việc>`, pull request nhỏ, build chạy được trước khi gộp.
- Mỗi nhóm chỉ cấu hình bảng của mình. Migration chỉ tạo từ nhánh đã cập nhật `main` mới nhất; xung đột migration thì người gộp sau tạo lại.
- Mọi thay đổi tồn kho đi qua `IStockService`. Không ai ghi thẳng vào `TonKho`.

## Contracts và bản giả (stub)

`Contracts/` chứa interface của các dịch vụ ở Bảng 1.6 của báo cáo. Mỗi interface có bản giả trong `Contracts/Stubs/` trả dữ liệu mẫu và cư xử đúng quy tắc (không tồn âm, vượt tồn bị từ chối kèm "cần bao nhiêu, còn bao nhiêu", sổ cái ghi đúng). Nhờ vậy các nhóm làm song song mà không chờ nhau.

| Interface | Nhóm sở hữu (đăng ký bản thật trong module của Area) |
|---|---|
| `IStockService`, `ICapacityService`, `ILockService`, `IAuditService`, `IGoiYService`, `INguoiDungHienTai`, `IPhanQuyenService` | HeThong (TV4) |
| `IHangHoaService` (`TaoLo`) | HangHoa (TV3) |
| `INhapKhoService` (`TaoPhieuNhapThua`, `TaoPhieuNhapNhap`) | NhapKho (TV1) |
| `IXuatKhoService` (`TaoPhieuXuatThieu`) | XuatKho (TV2) |
| `IGoiYHandler` của G1 / G2 / G3 và G4 | XuatKho / NhapKho / DieuChuyen |

Thay bản giả bằng bản thật: trong module Area của mình thêm một dòng, ví dụ `services.AddScoped<IStockService, StockService>();`. Đăng ký sau cùng thắng nên bản thật tự thay bản giả; **không đổi interface**. Tên phương thức giữ đúng như issue (không có hậu tố `Async`). Nơi gọi dịch vụ phải dùng interface, không `new` bản giả.

## Thêm bảng, migration và dữ liệu mẫu

`KhoDbContext` là `partial`. Nhóm thêm bảng của mình mà không sửa file chung:

1. Tạo `Areas/<Area>/Models/KhoDbContext.<Area>.cs`:

   ```csharp
   using Microsoft.EntityFrameworkCore;
   namespace QuanLyKho.Web.Data;

   public partial class KhoDbContext
   {
       public DbSet<Kho> Kho => Set<Kho>();
   }
   ```

2. Cấu hình bảng bằng lớp `IEntityTypeConfiguration<T>` đặt trong Area (tự được nạp). Tên bảng, cột, kiểu, ràng buộc theo báo cáo mục 2.8 (Bảng 2.15 đến 2.26).
3. Tạo migration (cài công cụ một lần: `dotnet tool install --global dotnet-ef`):

   ```
   dotnet ef migrations add TenMigration --project src/QuanLyKho.Web
   dotnet ef database update --project src/QuanLyKho.Web
   ```

4. Nạp dữ liệu mẫu: viết một lớp `IDbSeeder` cho bảng của mình, đọc từ `SeedDataSet`, đăng ký `services.AddScoped<IDbSeeder, ...>()` trong module Area, rồi bật `Database:SeedOnStartup` (xem `Data/IDbSeeder.cs`). Seeder phải chạy lại được nhiều lần.

`SeedData/SeedDataSet.cs` là nguồn dữ liệu mẫu duy nhất. Mật khẩu của 4 người dùng mẫu không nằm trong repo: `HeThongSeeder` tạo từ cấu hình `Auth:SeedPassword` (User Secrets hoặc biến môi trường `Auth__SeedPassword`).

## Menu theo vai trò

Mỗi Area có `Areas/<Area>/Services/<Area>Menu.cs` (implement `IMenuNguon`). Thêm mỗi màn hình một `MenuMuc` kèm danh sách vai trò được thấy, theo Bảng 2.2 của báo cáo. Menu chỉ ẩn/hiện mục; **kiểm tra quyền ở phía máy chủ** làm bằng `[CanQuyen(LoaiChungTu.XK, QuyenThaoTac.Duyet)]` trên Controller hoặc Action (xem `Areas/HeThong/README.md`), đừng dựa vào việc ẩn nút. Cả ứng dụng mặc định yêu cầu đăng nhập, nên Controller mới tự được bảo vệ.

## Thành phần quét mã

Mọi màn hình quét mã dùng chung một thành phần (Bảng 1.6 dòng 6):

```cshtml
<partial name="_QuetMa" model='new QuetMaOptions("oQuet", "Quét mã biến thể", "Mã vạch", "bienthe")' />

@section Scripts {
  <script>
    document.addEventListener("quetma:them", e => { /* e.detail.giaTri, e.detail.loaiMa, e.detail.nguon */ });
  </script>
}
```

Ô nhập tự lấy focus; Enter thêm một dòng rồi xóa ô và trả focus. Máy quét mã vạch hoặc ứng dụng quét trên điện thoại gõ mã và gửi Enter là dùng được ngay. Nút "Quét bằng camera" chỉ hiện khi trình duyệt hỗ trợ `BarcodeDetector` và trang mở bằng HTTPS (hoặc localhost). Trang thử: liên kết ở trang chủ (Development), hoặc `/HeThong/ThuNghiem/QuetMa`.

## Kiểm thử

```
dotnet test
```

Kiểm thử gồm: dữ liệu mẫu đúng ràng buộc CSDL, bản giả đúng quy tắc tồn kho (kể cả nhiều luồng cùng lấy hàng), sức chứa ô, khóa ô, vòng đời gợi ý AI, menu theo vai trò, và 5 Area tồn tại.

## Nộp việc (tóm tắt; đầy đủ trong issue của việc)

1. `git checkout main && git pull origin main` rồi `git checkout -b feature/tv<số>-<tên việc>`
2. Commit nhỏ, tiêu đề dạng `T4-1: mô tả ngắn (#41)`
3. Trước khi đẩy: `git fetch origin && git merge origin/main`, build lại, chạy lại bài demo
4. Mở Pull Request vào `main`, tiêu đề `[T4-1] Khung dự án - TV4 Lê Đức Anh`, mô tả có `Closes #41`, chọn một người khác làm Reviewer
