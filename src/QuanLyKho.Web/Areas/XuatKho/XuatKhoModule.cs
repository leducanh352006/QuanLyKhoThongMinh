using QuanLyKho.Web.Areas.XuatKho.Services;
using QuanLyKho.Web.Infrastructure.Menu;

namespace QuanLyKho.Web.Areas.XuatKho;

/// <summary>
/// Đăng ký dịch vụ của Area Xuất kho (Nhóm 3 – Xuất kho (kèm gợi ý G1)). Chỉ người phụ trách (TV2 Trần Tiến Đức) sửa file này,
/// nên Program.cs không bị xung đột khi gộp code. Đăng ký IXuatKhoService bản thật và IGoiYHandler của G1 trong XuatKhoModule.
/// </summary>
public static class XuatKhoModule
{
    public static IServiceCollection AddXuatKho(this IServiceCollection services)
    {
        services.AddSingleton<IMenuNguon, XuatKhoMenu>();
        return services;
    }
}
