using QuanLyKho.Web.Areas.HangHoa.Services;
using QuanLyKho.Web.Data;
using QuanLyKho.Web.Infrastructure.Menu;

namespace QuanLyKho.Web.Areas.HangHoa;

/// <summary>
/// Đăng ký dịch vụ của Area Hàng hóa (Nhóm 4 – Danh mục hàng hóa và kiểm tra hàng hóa). Chỉ người phụ trách (TV3 Dương Tuấn Anh) sửa file này,
/// nên Program.cs không bị xung đột khi gộp code. Đăng ký IHangHoaService bản thật trong HangHoaModule.
/// <para>Việc T3-1 (nhà cung cấp, danh mục sản phẩm) đăng ký ở đây: có SQL Server thì dùng EF Core; chưa cấu hình thì (chỉ Development)
/// dùng bản trong bộ nhớ nạp từ SeedData, cùng cách HeThongModule làm với Kho/ViTri.</para>
/// </summary>
public static class HangHoaModule
{
    public static IServiceCollection AddHangHoa(this IServiceCollection services)
    {
        services.AddSingleton<IMenuNguon, HangHoaMenu>();

        services.AddSingleton<BoNhoHangHoaStore>();
        services.AddScoped<IHangHoaStore>(sp =>
        {
            var db = sp.GetService<KhoDbContext>();
            if (db is not null) return new EfHangHoaStore(db);

            var moiTruong = sp.GetService<IHostEnvironment>();
            if (moiTruong is null || moiTruong.IsDevelopment()) return sp.GetRequiredService<BoNhoHangHoaStore>();
            throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:KhoDb nên không có nơi lưu nhà cung cấp, danh mục.");
        });
        services.AddScoped<IDbSeeder, HangHoaSeeder>();

        services.AddScoped<INhaCungCapQuanLy, NhaCungCapService>();
        services.AddScoped<IDanhMucQuanLy, DanhMucService>();

        return services;
    }
}
