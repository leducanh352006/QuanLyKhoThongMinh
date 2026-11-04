using QuanLyKho.Web.Areas.DieuChuyen.Services;
using QuanLyKho.Web.Infrastructure.Menu;

namespace QuanLyKho.Web.Areas.DieuChuyen;

/// <summary>
/// Đăng ký dịch vụ của Area Điều chuyển kho (Nhóm 5 – Điều chuyển giữa các kho). Chỉ người phụ trách (TV5 Nguyễn Minh Hiếu) sửa file này,
/// nên Program.cs không bị xung đột khi gộp code.
/// </summary>
public static class DieuChuyenModule
{
    public static IServiceCollection AddDieuChuyen(this IServiceCollection services)
    {
        services.AddSingleton<IMenuNguon, DieuChuyenMenu>();
        return services;
    }
}
