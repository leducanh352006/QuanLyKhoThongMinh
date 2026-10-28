using QuanLyKho.Web.Areas.NhapKho.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Contracts.Stubs;
using QuanLyKho.Web.Data;
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

        // T1-8: bản thật của INhapKhoService thay bản giả. Chưa cấu hình CSDL thì môi trường Development dùng lại bản giả, còn lại báo lỗi rõ ràng.
        services.AddSingleton<NhapKhoServiceStub>();
        services.AddScoped<INhapKhoService>(sp =>
        {
            var db = sp.GetService<KhoDbContext>();
            if (db is not null) return new NhapKhoService(db, sp.GetRequiredService<IStockService>(), sp.GetRequiredService<IAuditService>());

            var moiTruong = sp.GetService<IHostEnvironment>();
            if (moiTruong is null || moiTruong.IsDevelopment()) return sp.GetRequiredService<NhapKhoServiceStub>();
            throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:KhoDb nên không lưu được phiếu nhập. Xem README, mục \"Chạy lần đầu\".");
        });
        return services;
    }
}
