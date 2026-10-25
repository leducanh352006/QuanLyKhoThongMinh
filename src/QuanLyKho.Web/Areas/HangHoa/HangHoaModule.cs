using QuanLyKho.Web.Areas.HangHoa.Services;
using QuanLyKho.Web.Infrastructure.Menu;

namespace QuanLyKho.Web.Areas.HangHoa;

/// <summary>
/// Đăng ký dịch vụ của Area Hàng hóa (Nhóm 4 – Danh mục hàng hóa và kiểm tra hàng hóa). Chỉ người phụ trách (TV3 Dương Tuấn Anh) sửa file này,
/// nên Program.cs không bị xung đột khi gộp code. Đăng ký IHangHoaService bản thật trong HangHoaModule.
/// </summary>
public static class HangHoaModule
{
    public static IServiceCollection AddHangHoa(this IServiceCollection services)
    {
        services.AddSingleton<IMenuNguon, HangHoaMenu>();
        return services;
    }
}
