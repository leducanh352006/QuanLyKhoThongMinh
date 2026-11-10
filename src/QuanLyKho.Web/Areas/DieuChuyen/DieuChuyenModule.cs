using QuanLyKho.Web.Areas.DieuChuyen.Services;
using QuanLyKho.Web.Data;
using QuanLyKho.Web.Infrastructure.Menu;

namespace QuanLyKho.Web.Areas.DieuChuyen;

/// <summary>
/// Đăng ký dịch vụ của Area Điều chuyển kho (Nhóm 5 – Điều chuyển giữa các kho (kèm gợi ý G3, G4)). Chỉ người phụ trách (TV5 Nguyễn Minh Hiếu) sửa file này,
/// nên Program.cs không bị xung đột khi gộp code. Đăng ký IGoiYHandler của G3 và G4 trong DieuChuyenModule.
/// </summary>
public static class DieuChuyenModule
{
    public static IServiceCollection AddDieuChuyen(this IServiceCollection services)
    {
        services.AddSingleton<IMenuNguon, DieuChuyenMenu>();
        services.AddScoped<IDbSeeder, DieuChuyenSeeder>();
        // ITuyenVanChuyenQuanLy, IDinhMucKhoQuanLy KHÔNG đăng ký trực tiếp ở đây: cả hai phụ thuộc KhoDbContext không nullable,
        // mà DbContext chỉ được đăng ký khi có ConnectionStrings:KhoDb (xem AddKhoDatabase) — đăng ký thẳng sẽ làm ValidateOnBuild
        // báo lỗi ngay khi chưa cấu hình chuỗi kết nối. Controller tự dựng service từ KhoDbContext? db = null (theo đúng cách
        // Areas/XuatKho đã làm — xem DoiChieuController, XacNhanXuatController).
        return services;
    }
}
