using Microsoft.EntityFrameworkCore;

namespace QuanLyKho.Web.Data;

/// <summary>
/// Khởi tạo CSDL khi ứng dụng chạy, chỉ khi được bật trong cấu hình (mặc định tắt):
/// Database:MigrateOnStartup = true để áp migration, Database:SeedOnStartup = true để chạy các <see cref="IDbSeeder"/>.
/// </summary>
public sealed class DatabaseInitializer(IServiceScopeFactory scopes, IConfiguration config, ILogger<DatabaseInitializer> log) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        var migrate = config.GetValue<bool>("Database:MigrateOnStartup");
        var seed = config.GetValue<bool>("Database:SeedOnStartup");
        if (!migrate && !seed) return;

        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetService<KhoDbContext>();
        if (db is null)
        {
            log.LogWarning("Chưa cấu hình ConnectionStrings:KhoDb nên bỏ qua khởi tạo CSDL.");
            return;
        }

        if (migrate)
        {
            await db.Database.MigrateAsync(ct);
            log.LogInformation("Đã áp migration.");
        }

        if (seed)
        {
            var seeders = scope.ServiceProvider.GetServices<IDbSeeder>().OrderBy(s => s.ThuTu).ToList();
            if (seeders.Count == 0)
                log.LogWarning("Database:SeedOnStartup bật nhưng chưa có IDbSeeder nào được đăng ký (các nhóm thêm cùng bảng của mình).");
            foreach (var s in seeders)
            {
                await s.SeedAsync(db, ct);
                log.LogInformation("Đã chạy seeder {Seeder}.", s.GetType().Name);
            }
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
