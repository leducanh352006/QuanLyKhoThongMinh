using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Infrastructure.Database;

namespace QuanLyKho.Web.Data;

public static class DatabaseServiceExtensions
{
    public const string TenChuoiKetNoi = "KhoDb";

    /// <summary>
    /// Kết nối SQL Server qua chuỗi ConnectionStrings:KhoDb (User Secrets hoặc biến môi trường, không để trong git).
    /// Chưa có chuỗi thì không đăng ký DbContext và ứng dụng vẫn chạy (trang chủ báo "Chưa cấu hình").
    /// </summary>
    public static IServiceCollection AddKhoDatabase(this IServiceCollection services, IConfiguration config)
    {
        var chuoi = config.GetConnectionString(TenChuoiKetNoi);
        if (!string.IsNullOrWhiteSpace(chuoi))
            services.AddDbContext<KhoDbContext>(o => o.UseSqlServer(chuoi));

        services.AddScoped<IDatabaseStatusService, DatabaseStatusService>();
        services.AddHostedService<DatabaseInitializer>();
        return services;
    }
}
