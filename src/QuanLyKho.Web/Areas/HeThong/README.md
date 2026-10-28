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

---

# Kho và cây vị trí (việc T4-3, issue #39)

Báo cáo: Bảng 2.20 (Kho, ViTri), mục 2.8.1 (vị trí nhiều cấp; kích thước và khối lượng), Bảng 2.17 dòng 1 (hàng chỉ nằm ở ô, cấp con thấp hơn cấp cha).

## Đã làm

- CRUD Kho (`KhoController`, `KhoService`). Thêm kho **tự tạo kèm một ô Khu tiếp nhận** nên luôn đúng tiêu chí "mỗi kho có khu tiếp nhận" ngay từ lúc tạo.
- Cây vị trí nhiều cấp (`ViTriController`, `ViTriService`): thêm, sửa, khóa (xóa mềm), xóa cứng. Cấp con phải thấp hơn cấp cha và được phép bỏ qua cấp (seed "Khu tiếp nhận" là một ví dụ: một ô gốc, không qua Dãy/Kệ).
- Đường dẫn đầy đủ (`Kho ▷ Dãy A ▷ Kệ A1 ▷ Ô A1-01`) hiện **ngay trên cây**, không chỉ ở trang Sửa.
- Ràng buộc đã cài, khớp CHECK ở Bảng 2.20: chỉ ô có `LoaiViTri`, `MaVachO`, kích thước; ô Lưu trữ bắt buộc đủ 4 kích thước dương; ô Tiếp nhận và các cấp khác để trống các cột đó.
- Không khóa (xóa mềm) được khu tiếp nhận hoạt động cuối cùng của một kho, và không khóa được vị trí còn con đang hoạt động.
- Xóa cứng chỉ khi không còn vị trí con, và nếu là ô thì phải chưa có tồn hiện tại (qua `IStockService.LayTonTheoViTri`) — xem mục "Giới hạn đã biết" bên dưới.

## Cấu trúc dữ liệu

`ViTri` tự tham chiếu qua `MaViTriCha`. Để bảo đảm cấp con luôn cùng kho với cấp cha, bảng có khóa thay thế `(MaViTri, MaKho)` và khóa ngoại kép tự tham chiếu `(MaViTriCha, MaKho) → (MaViTri, MaKho)`. `MaViTriCha` cho phép NULL (gốc); SQL Server bỏ qua ràng buộc khóa ngoại kép khi một cột trong khóa là NULL, nên vị trí gốc không bị chặn.

## Các nhóm khác dùng thế nào

Không có hợp đồng mới trong `Contracts/`: các nhóm khác chỉ cần biết **mã** `MaKho`, `MaViTri` (đã có sẵn trong `SeedData/SeedDataSet.cs`, giữ nguyên qua `KhoViTriSeeder`). Muốn đọc thông tin vị trí (ví dụ hiển thị đường dẫn trên phiếu), tạm thời đọc thẳng `SeedDataSet.ViTri`/`TimViTri` như các bản giả trong `Contracts/Stubs` đang làm; khi cần một dịch vụ dùng chung thật sự (ví dụ `IViTriLookupService` để tránh phụ thuộc trực tiếp vào `Areas/HeThong`), hãy nêu trong issue #39 để bổ sung — chưa có nhu cầu cụ thể nên chưa thêm hợp đồng này.

## Giới hạn đã biết

- **Xóa cứng chỉ kiểm tồn hiện tại, chưa kiểm lịch sử sổ cái.** `IStockService` đã có bản thật (T4-4, xem mục bên dưới) nhưng `ViTriService.Xoa` vẫn chỉ gọi `LayTonTheoViTri` (tồn hiện tại = 0 thì cho xóa); một ô đã hết tồn nhưng còn lịch sử `BienDongTon` vẫn bị xóa cứng, mất vết. Nêu trong issue #39 nếu cần kiểm thêm lịch sử trước khi xóa.
- **Sửa vị trí chưa cho đổi cấp hoặc đổi cấp cha** (chỉ sửa tên, loại ô, mã vạch, kích thước). Di chuyển cả nhánh sang cấp cha khác chưa làm, vì phải kiểm lại ràng buộc cấp cho toàn bộ cây con — nêu trong issue #39 nếu cần.
- **Thêm kho và thêm khu tiếp nhận không chạy trong một giao dịch.** Nếu bước tạo khu tiếp nhận lỗi (rất hiếm, ví dụ trùng mã vạch tự sinh), kho vẫn được tạo và ứng dụng báo rõ để vào màn Vị trí thêm thủ công.
- **`DangKhoa` (khóa ô khi kiểm kê) chỉ là cột lưu trữ**, T4-3 không có màn hình thao tác cột này — đó là việc của nhóm Kiểm tra hàng hóa qua `ILockService` (đã có bản giả `LockServiceStub`).

## Kiểm thử

```
dotnet test --filter "FullyQualifiedName~KhoViTri"
```

47 test: thứ tự cấp vị trí (Bảng 2.16), ràng buộc kích thước theo loại ô, khu tiếp nhận tự tạo và không khóa được cái cuối cùng, xóa cứng chỉ khi chưa có tồn, dựng cây và đường dẫn đầy đủ, quy tắc cấu trúc Controller.

# Tồn kho và sổ cái (việc T4-4, issue #35)

Báo cáo: mục 2.8.1 (Tồn kho và sổ cái), Bảng 2.21.

## Đã làm

- Bảng `TonKho` (khóa `MaLo, MaViTri`: `SoLuongTon`, `SoLuongGiu`) và `BienDongTon` (sổ cái, chỉ thêm) qua EF Core Code First, FK tới `LoHang` (Area HangHoa) và `ViTri`/`NguoiDung` (Area này); ràng buộc CHECK khớp Bảng 2.16/2.17 (không âm, giữ không vượt tồn, số lượng sổ cái khác 0, miền giá trị LoaiBienDong/LoaiCT).
- `StockService : IStockService` (`Areas/HeThong/Services/StockService.cs`) thay `StockServiceStub` khi đã cấu hình `ConnectionStrings:KhoDb` (giống mẫu `IKhoViTriStore` ở T4-3); môi trường Development chưa cấu hình DB vẫn dùng `StockServiceStub`.
- Mỗi lần đổi tồn là một giao dịch (tham gia giao dịch của nơi gọi nếu đã mở sẵn trên cùng `KhoDbContext`), ghi đúng một dòng `BienDongTon` (hai dòng với `ChuyenO`) cùng lúc với `TonKho`.
- **Chống tranh chấp:** khóa lạc quan trên `TonKho.PhienBan` (rowversion) — câu UPDATE của EF luôn kèm điều kiện "còn đúng phiên bản vừa đọc"; thao tác thua thử lại tối đa 5 lần với số liệu mới nhất. Đã kiểm tay: 20 luồng cùng giữ chỗ 1 đơn vị trên tồn 10 cho ra đúng 10 thành công, 10 bị từ chối, không âm.
- **Chống ghi hai lần khi bị gọi lại:** trước khi ghi `GhiTang`/`GhiGiam`/`ChuyenO`, kiểm sổ cái đã có đúng dòng (chứng từ, loại biến động, lô, ô, số lượng) chưa; có rồi thì trả thành công mà không ghi lại.
- Hàng ở ô loại Tiếp nhận: `IStockService` không tự loại trừ loại ô khi tính tồn (đúng như bản giả) — nhóm Nhập kho chịu trách nhiệm không gọi `GhiTang` vào `available` cho tới khi hàng đã sắp xếp (xem báo cáo mục 2.8.1); `LayTonTheoViTri`/`DoiChieuSoCai` trả đúng số đã ghi, không ẩn ô Tiếp nhận.
- `TonKhoSeeder` (`ThuTu = 6`, chạy sau `LoHangSeeder`) nạp tồn mẫu kèm một dòng `BienDongTon` loại Nhận hàng (chứng từ giả NK số 0) cho mỗi dòng, để sổ cái khớp `TonKho` ngay từ đầu.
- Thêm migration `LoHangBangThat` tách riêng: bảng `LoHang` (T3-3) có cấu hình Entity nhưng chưa từng có migration tạo bảng trên `main`; đã tách để không lẫn vào migration của T4-4.

## Các nhóm khác dùng thế nào

Không đổi `IStockService`/`Contracts/Stubs/StockServiceStub` — bản thật thay bản giả qua DI, không ai cần sửa code đang gọi `IStockService`.

## Giới hạn đã biết

- **Không có idempotency key riêng.** Chống ghi hai lần dựa trên so khớp đúng (chứng từ, loại biến động, lô, ô, số lượng) đã có trong sổ cái; hai lời gọi hợp lệ nhưng khác số lượng cho cùng chứng từ (ví dụ sửa số lượng rồi gọi lại) sẽ ghi thêm, không bị coi là trùng.
- **`GiuCho`/`TraCho` không chống gọi lại trùng** (không ghi sổ cái nên rủi ro thấp hơn, nhưng gọi lại `GiuCho` hai lần sẽ giữ chỗ hai lần nếu nơi gọi không tự kiểm).
- **Chưa có màn hình** (xem việc Lịch sử biến động tồn, nêu trong issue #35 nếu cần trước).

## Kiểm thử

```
dotnet test --filter "FullyQualifiedName~StockServiceStub"
```

17 test hành vi bắt buộc (bản giả phải cư xử như bản thật): giữ/trả chỗ, vượt tồn bị từ chối kèm "cần bao nhiêu, còn bao nhiêu", ghi tăng/giảm đúng sổ cái, giảm từ phần giữ chỗ hoặc từ khả dụng, đổi ô tồn tổng không đổi, 100 luồng giữ chỗ đồng thời không làm tồn âm, đối chiếu sổ cái khớp `TonKho`. Bản thật (`StockService`, EF Core) đã kiểm tay trên SQL Server thật với cùng các tình huống (xem mục "Đã làm" phía trên) vì môi trường kiểm thử của dự án chưa có SQL Server/Sqlite cho các lớp `Ef*`, giống `EfKhoViTriStore` ở T4-3 cũng không có kiểm thử trực tiếp.
