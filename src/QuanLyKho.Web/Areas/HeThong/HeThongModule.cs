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

        // ---- Đăng nhập và phân quyền ----
        XacThucCookie.DangKy(services);
        services.AddAuthorization(o => o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        services.AddSingleton<IAuthorizationPolicyProvider, CanQuyenPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, CanQuyenHandler>();
        services.Configure<MvcOptions>(o => o.Conventions.Add(new MoTrangLoiChoMoiNguoi()));

        return services;
    }
}
