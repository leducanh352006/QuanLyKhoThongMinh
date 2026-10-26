using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Localization;
using QuanLyKho.Web.Areas.DieuChuyen;
using QuanLyKho.Web.Areas.HangHoa;
using QuanLyKho.Web.Areas.HeThong;
using QuanLyKho.Web.Areas.NhapKho;
using QuanLyKho.Web.Areas.XuatKho;
using QuanLyKho.Web.Contracts.Stubs;
using QuanLyKho.Web.Data;
using QuanLyKho.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Cho phép xuất chữ tiếng Việt nguyên dạng trong HTML (mặc định bị mã hóa thành &#x1EA3;).
builder.Services.AddWebEncoders(o => o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

// Hạ tầng dùng chung và cơ sở dữ liệu (chuỗi kết nối ConnectionStrings:KhoDb lấy từ User Secrets hoặc biến môi trường).
builder.Services.AddKhoInfrastructure();
builder.Services.AddKhoDatabase(builder.Configuration);

// Bản giả của mọi dịch vụ trong Contracts. PHẢI đứng trước các module Area:
// khi nhóm đăng ký bản thật trong module của mình, đăng ký sau cùng thắng nên bản thật tự thay bản giả.
builder.Services.AddContractStubs();

// Mỗi Area tự đăng ký dịch vụ của mình trong module riêng (chỉ người phụ trách sửa), không ai sửa Program.cs khi làm việc của mình.
builder.Services.AddHeThong();
builder.Services.AddNhapKho();
builder.Services.AddXuatKho();
builder.Services.AddHangHoa();
builder.Services.AddDieuChuyen();

var app = builder.Build();

// Số và ngày hiển thị theo vi-VN (ví dụ 1.250.000 ₫, dd/MM/yyyy); thời gian lưu UTC.
var viVN = new CultureInfo("vi-VN");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(viVN),
    SupportedCultures = new[] { viVN },
    SupportedUICultures = new[] { viVN },
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=TrangChu}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

/// <summary>Để dự án kiểm thử tham chiếu được điểm vào của ứng dụng.</summary>
public partial class Program { }
