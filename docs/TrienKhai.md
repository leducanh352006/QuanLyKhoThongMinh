# Triển khai lên máy chủ

Hai cách: máy chủ Windows chạy IIS (trong mạng nội bộ của công ty) hoặc Azure App Service (đám mây). Cả hai dùng gói lấy từ trang **Releases** của kho mã.

## Lưu ý chung

- Ứng dụng **bắt buộc chạy qua HTTPS** ở môi trường Production: cookie đăng nhập chỉ gửi qua HTTPS. Chạy bằng `http://` thì đăng nhập đúng mật khẩu vẫn quay lại trang đăng nhập.
- Cấu hình đặt bằng biến môi trường (không sửa file trong gói):

  | Biến môi trường | Giá trị | Bắt buộc |
  |---|---|---|
  | `ConnectionStrings__KhoDb` | Chuỗi kết nối tới database `QuanLyKho` | Có |
  | `ASPNETCORE_ENVIRONMENT` | `Production` | Có |
  | `Storage__TepDinhKem` | Thư mục lưu tệp đính kèm, ví dụ `D:\QuanLyKho\dinh-kem`. Mặc định là `App_Data\dinh-kem` trong thư mục ứng dụng | Không |
  | `Database__MigrateOnStartup` | `true` nếu muốn ứng dụng tự áp migration khi khởi động | Không |

- Không đặt `Database__SeedOnStartup` ở máy chủ thật: dữ liệu mẫu chỉ dành cho máy phát triển.
- Database: tạo database rỗng `QuanLyKho`, chạy tệp `QuanLyKho-<phiên bản>-csdl.sql` trong bản phát hành (SSMS hoặc `sqlcmd -I -i`). Script chạy lại được nhiều lần, chỉ áp phần còn thiếu, nên dùng luôn cho các lần nâng cấp sau. Khi nâng cấp CSDL cũ, migration `KhoaNgoaiPhieuKiem` sẽ lỗi nếu `PhieuNhap.MaPK` hoặc `PhieuXuat.MaPK` trỏ tới phiếu kiểm không tồn tại: tìm bằng `SELECT MaPN FROM PhieuNhap WHERE MaPK IS NOT NULL AND MaPK NOT IN (SELECT MaPK FROM PhieuKiem)` (tương tự với `PhieuXuat`), sửa về `NULL` rồi chạy lại.
- `TrustServerCertificate=True` trong các chuỗi kết nối mẫu chỉ dùng khi chạy thử với chứng chỉ tự ký của SQL Server. Máy chủ thật nên cài chứng chỉ hợp lệ cho SQL Server và bỏ tham số này.
- Tài khoản SQL dùng cho ứng dụng nên là tài khoản riêng chỉ có quyền `db_datareader`, `db_datawriter` trên `QuanLyKho`, không dùng `sa`.

## Cách 1: IIS trên Windows Server hoặc Windows 10/11

1. Bật IIS: *Turn Windows features on or off* > *Internet Information Services* (Windows Server: *Server Manager* > *Add Roles* > *Web Server (IIS)*).
2. Cài **ASP.NET Core 10 Hosting Bundle** từ trang tải .NET 10 của Microsoft. Cài xong chạy `iisreset` trong cửa sổ lệnh quyền Administrator.
3. Tải `QuanLyKho-<phiên bản>-win-x64.zip` từ trang Releases, giải nén vào `C:\inetpub\QuanLyKho`.
4. Tạo database theo mục "Lưu ý chung".
5. Mở *IIS Manager*:
   - *Application Pools* > *Add Application Pool*: tên `QuanLyKho`, *.NET CLR version* chọn **No Managed Code**.
   - *Sites* > *Add Website*: tên `QuanLyKho`, *Physical path* `C:\inetpub\QuanLyKho`, chọn pool `QuanLyKho`, *Binding* chọn `https`, cổng 443, chọn chứng chỉ SSL (chứng chỉ của công ty, hoặc chứng chỉ tự ký tạo ở *Server Certificates* để chạy thử).
6. Đặt biến môi trường cho site: chọn site > *Configuration Editor* > section `system.webServer/aspNetCore` > `environmentVariables` > thêm `ConnectionStrings__KhoDb` và `ASPNETCORE_ENVIRONMENT=Production`. Cách khác: sửa thẳng `web.config` trong thư mục ứng dụng:

   ```xml
   <aspNetCore processPath="dotnet" arguments=".\QuanLyKho.Web.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess">
     <environmentVariables>
       <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
       <environmentVariable name="ConnectionStrings__KhoDb" value="Server=TEN-MAY\SQLEXPRESS;Database=QuanLyKho;User ID=quanlykho_app;Password=...;Encrypt=True;TrustServerCertificate=True" />
     </environmentVariables>
   </aspNetCore>
   ```

7. Cấp quyền ghi cho thư mục tệp đính kèm: chuột phải `C:\inetpub\QuanLyKho\App_Data` (tạo nếu chưa có) > *Properties* > *Security* > *Edit* > *Add* `IIS AppPool\QuanLyKho` > tick *Modify*.
8. SQL Server cùng máy và dùng Windows Authentication: thêm login `IIS AppPool\QuanLyKho` trong SSMS rồi đổi chuỗi kết nối sang `Trusted_Connection=True`. SQL Server máy khác: dùng tài khoản SQL như ví dụ trên và mở cổng 1433 ở tường lửa máy SQL.
9. Mở `https://<tên-máy>/`, đăng nhập. Lỗi 500.30 hoặc 502.5: bật `stdoutLogEnabled="true"` trong `web.config`, xem tệp trong `logs\`, thường là sai chuỗi kết nối hoặc chưa cài Hosting Bundle.

Nâng cấp phiên bản: dừng site (hoặc đặt tệp `app_offline.htm` vào thư mục), chép đè tệp mới (giữ `web.config` và `App_Data`), chạy script CSDL của bản mới, chạy lại site.

## Cách 2: Azure App Service

1. Tạo **Azure SQL Database** tên `QuanLyKho` (gói Basic là đủ chạy thử). Ở *Networking* bật *Allow Azure services and resources to access this server* và thêm IP máy mình để chạy script.
2. Chạy script CSDL trong bản phát hành lên Azure SQL bằng SSMS hoặc *Query editor* của Azure.
3. Tạo **App Service**: *Publish* = Code, *Runtime stack* = .NET 10, *Operating System* = Windows hoặc Linux.
4. *Settings* > *Environment variables*:
   - *Connection strings*: tên `KhoDb`, loại `SQLAzure`, giá trị lấy từ trang Azure SQL (*Connection strings* > ADO.NET).
   - *App settings*: `ASPNETCORE_ENVIRONMENT` = `Production`; `Storage__TepDinhKem` = `/home/dinh-kem` (Linux) hoặc `D:\home\dinh-kem` (Windows) để tệp đính kèm không mất khi triển khai lại.
5. Đưa gói lên: *Deployment Center* > chọn GitHub, kho `QuanLyKhoThongMinh`, nhánh `main` (Azure tự tạo workflow build và triển khai). Cách khác là dùng Azure CLI với gói đã tải:

   ```
   az webapp deploy --resource-group <nhom> --name <ten-app> --src-path QuanLyKho-v1.0.0-linux-x64.zip --type zip
   ```

6. App Service có sẵn HTTPS (`https://<ten-app>.azurewebsites.net`). Bật *HTTPS Only* ở *Configuration* > *General settings*.
7. Xem lỗi khởi động ở *Log stream* hoặc *Diagnose and solve problems*.

## Kiểm tra sau khi triển khai

1. Mở trang chủ qua HTTPS, đăng nhập lần lượt bốn vai trò.
2. Trang chủ hiện trạng thái kết nối SQL Server: phải báo đã kết nối.
3. Lập một phiếu nhập nháp rồi hủy (có lý do) để thử ghi CSDL và nhật ký.
4. Thử đính kèm một tệp nhỏ để chắc thư mục tệp có quyền ghi.
