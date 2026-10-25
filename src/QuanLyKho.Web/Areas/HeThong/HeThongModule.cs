using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Infrastructure.Menu;

namespace QuanLyKho.Web.Areas.HeThong;

/// <summary>
/// Đăng ký dịch vụ của Area Hệ thống (Nhóm 1 – Hệ thống nền, kho và tồn kho). Chỉ người phụ trách (TV4 Lê Đức Anh) sửa file này,
/// nên Program.cs không bị xung đột khi gộp code. Dịch vụ tồn kho, sổ cái, sức chứa, khóa ô, nhật ký và khung gợi ý AI nằm ở đây; khi xong, đăng ký bản thật trong HeThongModule để thay bản giả.
/// </summary>
public static class HeThongModule
{
    public static IServiceCollection AddHeThong(this IServiceCollection services)
    {
        services.AddSingleton<IMenuNguon, HeThongMenu>();
        return services;
    }
}
