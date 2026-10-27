# Area HeThong: đăng nhập, người dùng, phân quyền (việc T4-2, issue #40)

Người phụ trách: TV4 Lê Đức Anh. Báo cáo: Bảng 2.2 (vai trò), 2.16 và 2.17 (miền giá trị, ràng buộc), 2.19 (VaiTro, PhanQuyen, NguoiDung).

## Đã làm

- Đăng nhập, đăng xuất bằng cookie. Mật khẩu chỉ lưu bản băm (PBKDF2 của ASP.NET Core, cột `MatKhauHash`).
- Bốn vai trò: Quản trị viên, Quản lý kho, Thủ kho, Nhân viên bán hàng.
- Phân quyền theo vai trò và loại chứng từ (NK, XK, CK, KK) với năm cờ Xem, Thêm, Sửa, Xóa, Duyệt, kiểm ở **phía máy chủ**.
- Hàm chung chặn người lập tự duyệt phiếu của mình.
- Màn hình: đăng nhập, quản lý người dùng (thêm, sửa, khóa, mở khóa, đặt lại mật khẩu), gán quyền theo vai trò.
- Cả ứng dụng mặc định **yêu cầu đăng nhập**. Controller mới của mọi nhóm tự được bảo vệ, không cần nhớ gắn `[Authorize]`.

## Các nhóm khác dùng thế nào

Có ba công cụ, đều nằm trong `Contracts/PhanQuyen.cs`. Không ai cần sửa `Program.cs` hay file của Area này.

### 1. Thuộc tính `[CanQuyen]`: chặn theo quyền chứng từ

```csharp
[Area("XuatKho")]
public class PhieuXuatController : Controller
{
    [CanQuyen(LoaiChungTu.XK, QuyenThaoTac.Xem)]
    public IActionResult Index() => View();

    [HttpPost, ValidateAntiForgeryToken]
    [CanQuyen(LoaiChungTu.XK, QuyenThaoTac.Duyet)]
    public async Task<IActionResult> Duyet(int id) { /* ... */ }
}
```

- Đặt trên Controller hoặc Action. Đặt nhiều thuộc tính thì phải thỏa **tất cả**.
- Chưa đăng nhập thì chuyển tới trang đăng nhập. Đã đăng nhập mà thiếu quyền thì vào trang "Không đủ quyền" (mã 403).
- Gọi bằng AJAX/fetch (header `X-Requested-With: XMLHttpRequest` hoặc `Accept: application/json`) thì trả thẳng 401 hoặc 403, không chuyển hướng.
- Quyền đọc từ bảng `PhanQuyen`, có nhớ tạm 5 phút và được làm mới ngay khi quản trị viên lưu phân quyền. Không đăng nhập lại cũng có hiệu lực.

### 2. `IPhanQuyenService`: hỏi quyền bằng code trong Service

```csharp
var kq = await phanQuyen.KiemTra(LoaiChungTu.XK, QuyenThaoTac.Duyet, ct);
if (!kq.ThanhCong) return kq;                 // kq.ThongBao: "Bạn không có quyền duyệt phiếu xuất."
bool co = await phanQuyen.CoQuyen(maVaiTro, LoaiChungTu.CK, QuyenThaoTac.Them, ct);
```

### 3. `QuyTacDuyet`: người lập không tự duyệt (Bảng 2.17 dòng 8)

Gọi ở phía máy chủ ngay trước khi ghi trạng thái duyệt:

```csharp
var guard = QuyTacDuyet.KiemTra(hienTai.Lay()!.MaND, phieu.MaNguoiLap, "phiếu xuất");   // phiếu xuất: MaNguoiLap
if (!guard.ThanhCong) return guard;                                                      // MaLoi = "TU_DUYET"
// dòng kiểm kê: truyền MaNguoiDem của DongKiem thay cho MaNguoiLap
```

### Việc khác hay gặp

| Cần | Làm |
|---|---|
| Biết ai đang thao tác | Inject `INguoiDungHienTai` (đã là bản thật, đọc từ cookie). `Lay()` trả `null` nếu chưa đăng nhập. |
| Mở một action cho người chưa đăng nhập | `[AllowAnonymous]` (chỉ nên dùng cho đăng nhập, trang lỗi). |
| Chỉ cho một vai trò | `[Authorize(Roles = VaiTroTen.QuanTriVien)]` (dùng cho việc không gắn với chứng từ, như quản trị). |
| Ghi nhật ký thao tác | `IAuditService.Ghi(...)` như cũ. |

Lưu ý: POST nào cũng nên có `[ValidateAntiForgeryToken]` (form Razor tự sinh token). Không đặt `[CanQuyen]` thay cho kiểm tra trạng thái phiếu; chúng là hai lớp khác nhau.

## Tài khoản mẫu và cấu hình

Mật khẩu mẫu **không nằm trong repo**. Đặt bằng User Secrets hoặc biến môi trường (cùng một mật khẩu cho cả 4 tài khoản, cần ít nhất 8 ký tự, có chữ và số):

```
cd src/QuanLyKho.Web
dotnet user-secrets set "Auth:SeedPassword" "<mật khẩu bạn chọn>"
```

Hoặc `Auth__SeedPassword=...` làm biến môi trường. Thiếu hoặc yếu thì không tạo tài khoản mẫu và ứng dụng ghi cảnh báo.

| Tài khoản | Vai trò |
|---|---|
| `quantri` | Quản trị viên |
| `quanlykho` | Quản lý kho |
| `thukho` | Thủ kho |
| `nhanvienbh` | Nhân viên bán hàng |

**Nơi lưu người dùng:**

- Có `ConnectionStrings:KhoDb`: dùng SQL Server qua EF Core. Phải áp migration và nạp dữ liệu mẫu (mục dưới).
- Chưa có chuỗi kết nối **và** môi trường Development: dùng kho trong bộ nhớ nạp từ `SeedData` (mất khi tắt ứng dụng), để mọi người đăng nhập và làm việc ngay.
- Chưa có chuỗi kết nối và không phải Development: không chạy (báo lỗi rõ ràng), để không vô tình chạy thật mà không có nơi lưu.

## Ma trận quyền mặc định

Nạp lúc seed, chỉ thêm dòng còn thiếu, **không đè** quyền quản trị viên đã chỉnh. Xem `Services/QuyenMacDinh.cs`.

| Vai trò | NK (nhập) | XK (xuất) | CK (chuyển) | KK (kiểm kê) |
|---|---|---|---|---|
| Quản trị viên | Xem Thêm Sửa Xóa Duyệt | Xem Thêm Sửa Xóa Duyệt | Xem Thêm Sửa Xóa Duyệt | Xem Thêm Sửa Xóa (không Duyệt) |
| Quản lý kho | tất cả 5 quyền | tất cả 5 quyền | tất cả 5 quyền | tất cả 5 quyền |
| Thủ kho | Xem Thêm Sửa Xóa Duyệt | Xem Thêm Sửa | Xem Sửa | Xem Thêm Sửa |
| Nhân viên bán hàng | không | Xem Thêm | không | không |

**GIẢ ĐỊNH cần nhóm xác nhận.** Bảng 2.2 mô tả nhiệm vụ bằng lời, chưa nói rõ từng cờ, nên các ô trên là cách quy đổi của T4-2:

- Thủ kho có Duyệt trên NK vì duyệt gợi ý G2 (xếp hàng) thuộc phiếu nhập. Quản lý kho có Duyệt trên XK, CK, KK vì duyệt phiếu xuất cùng G1, G4 và phát sinh kiểm kê.
- Quản trị viên "toàn quyền" theo Bảng 2.2, trừ Duyệt phát sinh kiểm kê.
- Ma trận chỉ có 4 loại chứng từ nên **chưa có cờ riêng** cho: duyệt gợi ý G1 đến G4, lấy hàng, quét xác nhận, "xem tồn và vị trí" của Nhân viên bán hàng. Các nhóm nên chốt gắn vào cờ nào (ví dụ G1 gắn XK.Duyệt, G4 gắn CK.Duyệt) rồi dùng `[CanQuyen]` tương ứng.
- Quyền duyệt phát sinh của phiếu kiểm **chỉ gán cho Quản lý kho** (Bảng 2.2, ghi chú dưới bảng). Hệ thống từ chối lưu nếu gán cho vai trò khác.

## Quy tắc đã cài (thêm ngoài danh sách của issue, để tránh khóa nhầm hệ thống)

- Có Thêm, Sửa, Xóa hoặc Duyệt thì phải có Xem.
- Không tự khóa tài khoản của mình, không tự đổi vai trò của mình.
- Không khóa hoặc hạ vai trò Quản trị viên hoạt động cuối cùng.
- Khóa tài khoản hoặc đổi vai trò có hiệu lực **ngay** với phiên đang đăng nhập: mỗi yêu cầu kiểm lại tài khoản (nhớ tạm 30 giây, xóa ngay khi khóa hoặc đổi vai trò).
- Sai mật khẩu 5 lần trong 15 phút thì khóa đăng nhập của cặp (tên đăng nhập, IP) 15 phút. Thông báo sai mật khẩu và tên không tồn tại giống hệt nhau.
- Màn hình quản lý người dùng và phân quyền kiểm theo **vai trò** (không theo bảng PhanQuyen), nên không ai tự gỡ quyền của chính các màn hình này mà bị khóa ngoài.
- Nhật ký: đăng nhập, đăng xuất, thêm, sửa, khóa, mở khóa, đặt lại mật khẩu, đổi phân quyền (không ghi mật khẩu).

## Dùng SQL Server

1. Cập nhật nhánh theo `main` mới nhất, rồi tạo migration (xung đột migration thì người gộp sau tạo lại, xem README gốc):

   ```
   dotnet ef migrations add HeThongNguoiDungPhanQuyen --project src/QuanLyKho.Web
   ```

2. Trong `appsettings.Development.json` bật `Database:MigrateOnStartup` và `Database:SeedOnStartup`, đặt `Auth:SeedPassword`, rồi chạy. `HeThongSeeder` chạy đầu tiên (`ThuTu = 1`), giữ nguyên `MaVaiTro`, `MaND` của `SeedData` (ví dụ `Kho.MaQuanLy = 2`), chạy lại nhiều lần không sao.

## Cấu trúc

```
Areas/HeThong/
  Controllers/   TaiKhoan (đăng nhập), NguoiDung, PhanQuyen, TrangChu, ThuNghiem (chỉ Development)
  Models/        Entity + cấu hình EF (VaiTro, PhanQuyen, NguoiDung), DbSet, ViewModel
  Services/      MatKhauService, TaiKhoanService, PhanQuyenService, NguoiDungQuanLyService,
                 IHeThongStore (EfHeThongStore, BoNhoHeThongStore), HeThongSeeder,
                 XacThucCookie, CanQuyenAuthorization, QuyenMacDinh
  Views/         TaiKhoan, NguoiDung, PhanQuyen
Contracts/PhanQuyen.cs    IPhanQuyenService, [CanQuyen], QuyTacDuyet
```

Nghiệp vụ chỉ biết `IHeThongStore`, nên kiểm thử chạy được không cần SQL Server.

## Kiểm thử

```
dotnet test --filter "FullyQualifiedName~HeThong"
```

Gồm mật khẩu, đăng nhập và chặn dò mật khẩu, phiên, quản lý người dùng, ma trận quyền, chính sách `[CanQuyen]` qua DI, và quy tắc cấu trúc (Controller của Area này phải có kiểm quyền, POST phải có chống giả mạo yêu cầu).

## Chưa làm, để việc sau

- `IAuditService` vẫn là bản giả của T4-1 (nhật ký trong bộ nhớ). Khi có bản thật ghi vào `NhatKyThaoTac`, các lệnh ghi nhật ký ở đây tự đi vào bảng.
- Đổi mật khẩu bởi chính người dùng, quên mật khẩu qua email, đăng nhập nhiều bước.
- Hủy phiên đang mở khi đặt lại mật khẩu (hiện chỉ khóa tài khoản và đổi vai trò có hiệu lực ngay). Bảng NguoiDung theo báo cáo chưa có cột dấu phiên (security stamp).
- Chống giả mạo yêu cầu (CSRF) tự động cho toàn ứng dụng: hiện từng POST của Area này gắn `[ValidateAntiForgeryToken]`. Có thể bật bộ lọc `AutoValidateAntiforgeryToken` toàn cục khi các nhóm đã quen.
