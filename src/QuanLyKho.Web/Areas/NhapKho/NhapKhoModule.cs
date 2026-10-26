using QuanLyKho.Web.Areas.NhapKho.Services;
using QuanLyKho.Web.Infrastructure.Menu;

namespace QuanLyKho.Web.Areas.NhapKho;

/// <summary>
/// Đăng ký dịch vụ của Area Nhập kho (Nhóm 2 – Nhập kho (kèm gợi ý G2)). Chỉ người phụ trách (TV1 Nguyễn Văn Hiếu) sửa file này,
/// nên Program.cs không bị xung đột khi gộp code. Đăng ký INhapKhoService bản thật và IGoiYHandler của G2 trong NhapKhoModule.
/// </summary>
public static class NhapKhoModule
{
    public static IServiceCollection AddNhapKho(this IServiceCollection services)
    {
        services.AddSingleton<IMenuNguon, NhapKhoMenu>();
        return services;
    }
}
