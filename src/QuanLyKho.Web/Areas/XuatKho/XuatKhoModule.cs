using QuanLyKho.Web.Areas.XuatKho.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Contracts.Stubs;
using QuanLyKho.Web.Data;
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

        // T2-8: bản thật của IXuatKhoService thay bản giả. Chưa cấu hình CSDL thì môi trường Development dùng lại bản giả, còn lại báo lỗi rõ ràng.
        services.AddSingleton<XuatKhoServiceStub>();
        services.AddScoped<IXuatKhoService>(sp =>
        {
            var db = sp.GetService<KhoDbContext>();
            if (db is not null) return new XuatKhoService(db, sp.GetRequiredService<IStockService>(), sp.GetRequiredService<IAuditService>());

            var moiTruong = sp.GetService<IHostEnvironment>();
            if (moiTruong is null || moiTruong.IsDevelopment()) return sp.GetRequiredService<XuatKhoServiceStub>();
            throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:KhoDb nên không lưu được phiếu xuất. Xem README, mục \"Chạy lần đầu\".");
        });

        // T2-2: phân bổ FEFO/FIFO và dựng đề xuất gợi ý G1 (dùng chung cho TaoGoiY và Duyet tay trực tiếp).
        services.AddScoped<IGoiYG1Service, GoiYG1Service>();

        // T2-2: bộ xử lý gợi ý G1 (lấy hàng từ lô, ô nào) cho khung gợi ý AI dùng chung (T4-8, xem README của Area HeThong,
        // mục "Các nhóm khác dùng thế nào" của T4-8). Luôn đăng ký (khung giải quyết IEnumerable<IGoiYHandler> ngay khi dựng
        // GoiYService nên không được ném lỗi ở đây); thiếu KhoDbContext thì HandlerG1 tự báo lỗi rõ ràng khi thật sự được gọi.
        services.AddScoped<IGoiYHandler, HandlerG1>();
        return services;
    }
}
