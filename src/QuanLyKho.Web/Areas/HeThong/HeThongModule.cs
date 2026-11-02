using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Contracts.Stubs;
using QuanLyKho.Web.Data;
using QuanLyKho.Web.Infrastructure.Menu;

namespace QuanLyKho.Web.Areas.HeThong;

/// <summary>
/// Đăng ký dịch vụ của Area Hệ thống (Nhóm 1 – Hệ thống nền, kho và tồn kho). Chỉ người phụ trách (TV4 Lê Đức Anh) sửa file này,
/// nên Program.cs không bị xung đột khi gộp code.
/// <para>Việc T4-2 (đăng nhập, người dùng, phân quyền) đăng ký ở đây: cookie đăng nhập, <see cref="INguoiDungHienTai"/> thật (thay bản giả của T4-1),
/// <see cref="IPhanQuyenService"/>, thuộc tính <c>[CanQuyen]</c> và chính sách mặc định "phải đăng nhập" cho toàn ứng dụng.
/// Việc kho, vị trí, tồn kho, sổ cái, sức chứa, khóa ô, nhật ký và khung gợi ý AI sẽ thêm bản thật vào đây để thay bản giả.</para>
/// </summary>
public static class HeThongModule
{
    public static IServiceCollection AddHeThong(this IServiceCollection services)
    {
        services.AddSingleton<IMenuNguon, HeThongMenu>();

        // ---- Dịch vụ dùng chung ----
        services.AddMemoryCache();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IMatKhauService, MatKhauService>();
        services.AddSingleton<DangNhapGioiHan>();

        // ---- Dữ liệu: có SQL Server thì dùng EF Core; chưa cấu hình thì (chỉ Development) dùng bản trong bộ nhớ nạp từ SeedData ----
        services.AddSingleton<BoNhoHeThongStore>();
        services.AddScoped<IHeThongStore>(sp =>
        {
            var db = sp.GetService<KhoDbContext>();
            if (db is not null) return new EfHeThongStore(db);

            var moiTruong = sp.GetService<IHostEnvironment>();
            if (moiTruong is null || moiTruong.IsDevelopment()) return sp.GetRequiredService<BoNhoHeThongStore>();
            throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:KhoDb nên không có nơi lưu người dùng. Xem README, mục \"Chạy lần đầu\".");
        });
        services.AddScoped<IDbSeeder, HeThongSeeder>();
        services.AddSingleton<BoNhoKhoViTriStore>();
        services.AddScoped<IKhoViTriStore>(sp =>
        {
            var db = sp.GetService<KhoDbContext>();
            if (db is not null) return new EfKhoViTriStore(db);

            var moiTruong = sp.GetService<IHostEnvironment>();
            if (moiTruong is null || moiTruong.IsDevelopment()) return sp.GetRequiredService<BoNhoKhoViTriStore>();
            throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:KhoDb nên không có nơi lưu kho, vị trí. Xem README, mục \"Chạy lần đầu\".");
        });
        services.AddScoped<IDbSeeder, KhoViTriSeeder>();
        services.AddSingleton<StockServiceStub>();
        services.AddScoped<IStockService>(sp =>
        {
            var db = sp.GetService<KhoDbContext>();
            if (db is not null) return new StockService(db);

            var moiTruong = sp.GetService<IHostEnvironment>();
            if (moiTruong is null || moiTruong.IsDevelopment()) return sp.GetRequiredService<StockServiceStub>();
            throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:KhoDb nên không có nơi lưu tồn kho. Xem README, mục \"Chạy lần đầu\".");
        });
        services.AddScoped<IDbSeeder, TonKhoSeeder>();   // T4-4: thay bản giả StockServiceStub, TonKho/BienDongTon qua EF Core

        // ---- Nghiệp vụ ----
        services.AddScoped<ITaiKhoanService, TaiKhoanService>();
        services.AddScoped<IPhanQuyenQuanLy, PhanQuyenService>();
        services.AddScoped<IPhanQuyenService>(sp => sp.GetRequiredService<IPhanQuyenQuanLy>());
        services.AddScoped<INguoiDungQuanLy, NguoiDungQuanLyService>();
        services.AddScoped<IKhoQuanLy, KhoService>();
        services.AddScoped<IViTriQuanLy, ViTriService>();
        services.AddScoped<ILockService, LockService>();   // T4-5: thay bản giả LockServiceStub, khóa/mở ô qua ViTri.DangKhoa

        // T4-7: nhật ký thao tác và tệp đính kèm. Bản thật của IAuditService thay AuditServiceStub; chưa có CSDL thì (chỉ Development) dùng bản trong bộ nhớ.
        services.AddSingleton<BoNhoNhatKyStore>();
        services.AddScoped<INhatKyStore>(sp =>
        {
            var db = sp.GetService<KhoDbContext>();
            if (db is not null) return new EfNhatKyStore(db);

            var moiTruong = sp.GetService<IHostEnvironment>();
            if (moiTruong is null || moiTruong.IsDevelopment()) return sp.GetRequiredService<BoNhoNhatKyStore>();
            throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:KhoDb nên không có nơi lưu nhật ký. Xem README, mục \"Chạy lần đầu\".");
        });
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<INhatKyQuanLy, NhatKyService>();
        services.AddSingleton<BoNhoTepDinhKemStore>();
        services.AddScoped<ITepDinhKemStore>(sp =>
        {
            var db = sp.GetService<KhoDbContext>();
            if (db is not null) return new EfTepDinhKemStore(db);

            var moiTruong = sp.GetService<IHostEnvironment>();
            if (moiTruong is null || moiTruong.IsDevelopment()) return sp.GetRequiredService<BoNhoTepDinhKemStore>();
            throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:KhoDb nên không có nơi lưu tệp đính kèm. Xem README, mục \"Chạy lần đầu\".");
        });
        services.AddSingleton(sp =>
        {
            // Thư mục lưu tệp nằm ngoài wwwroot (không ai tải trực tiếp được) và ngoài git; đổi bằng cấu hình Storage:TepDinhKem.
            var goc = sp.GetRequiredService<IHostEnvironment>().ContentRootPath;
            var thuMuc = sp.GetRequiredService<IConfiguration>()["Storage:TepDinhKem"];
            return new TepLuuTruCauHinh(Path.GetFullPath(string.IsNullOrWhiteSpace(thuMuc) ? Path.Combine("App_Data", "dinh-kem") : thuMuc, goc));
        });
        services.AddScoped<IChungTuTonTai, PhieuNhapTonTai>();   // các nhóm khác đăng ký bản cho XK, CK, KK của mình
        services.AddScoped<ITepDinhKemQuanLy, TepDinhKemService>();
        services.TryAddScoped<INguonHangChoXep, KhongCoHangChoXep>();   // T4-5: Nhóm 2 đăng ký nguồn thật sau (đăng ký sau cùng thắng)
        services.AddScoped<ICapacityService>(sp =>   // T4-5: thay bản giả CapacityServiceStub
        {
            var db = sp.GetService<KhoDbContext>();
            if (db is not null) return new CapacityService(db, sp.GetRequiredService<INguonHangChoXep>());

            var moiTruong = sp.GetService<IHostEnvironment>();
            if (moiTruong is null || moiTruong.IsDevelopment()) return ActivatorUtilities.CreateInstance<CapacityServiceStub>(sp);
            throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:KhoDb nên không kiểm tra được sức chứa. Xem README, mục \"Chạy lần đầu\".");
        });
        services.AddScoped<INguoiDungHienTai, NguoiDungHienTaiService>();   // đăng ký sau AddKhoInfrastructure nên thay bản giả Development

        // T4-8: khung gợi ý AI. Bản thật của IGoiYService thay GoiYServiceStub; chưa có CSDL thì (chỉ Development) lưu trong bộ nhớ.
        services.AddSingleton<BoNhoGoiYStore>();
        services.AddScoped<IGoiYStore>(sp =>
        {
            var db = sp.GetService<KhoDbContext>();
            if (db is not null) return new EfGoiYStore(db);

            var moiTruong = sp.GetService<IHostEnvironment>();
            if (moiTruong is null || moiTruong.IsDevelopment()) return sp.GetRequiredService<BoNhoGoiYStore>();
            throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:KhoDb nên không có nơi lưu gợi ý AI. Xem README, mục \"Chạy lần đầu\".");
        });
        services.AddScoped<GoiYService>();
        services.AddScoped<IGoiYService>(sp => sp.GetRequiredService<GoiYService>());
        services.AddScoped<IGoiYQuanLy>(sp => sp.GetRequiredService<GoiYService>());
        // Gợi ý mẫu và handler mẫu để các nhóm thử: handler mẫu chỉ bật ở Development và luôn nhường handler thật của nhóm sở hữu.
        services.AddSingleton<ThaoTacMauStore>();
        foreach (var loai in Enum.GetValues<LoaiGoiY>())
            services.AddScoped<IGoiYHandler>(sp => ActivatorUtilities.CreateInstance<GoiYHandlerMau>(sp, loai));
        services.AddScoped<GoiYMauService>();

        // ---- Đăng nhập và phân quyền ----
        XacThucCookie.DangKy(services);
        services.AddAuthorization(o => o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        services.AddSingleton<IAuthorizationPolicyProvider, CanQuyenPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, CanQuyenHandler>();
        services.Configure<MvcOptions>(o => o.Conventions.Add(new MoTrangLoiChoMoiNguoi()));

        return services;
    }
}
