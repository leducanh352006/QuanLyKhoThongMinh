using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Infrastructure.Database;

namespace QuanLyKho.Web.Data;

public sealed class DatabaseStatusService(IServiceProvider sp) : IDatabaseStatusService
{
    public async Task<DatabaseStatus> KiemTraAsync(CancellationToken ct = default)
    {
        var db = sp.GetService<KhoDbContext>();
        if (db is null) return DatabaseStatus.ChuaCauHinh();

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));
            return await db.Database.CanConnectAsync(cts.Token)
                ? DatabaseStatus.DaKetNoi()
                : DatabaseStatus.Loi("Không kết nối được tới SQL Server; kiểm tra dịch vụ SQL Server đã chạy và chuỗi kết nối.");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return DatabaseStatus.Loi("Hết thời gian chờ (10 giây) khi nối SQL Server: kiểm tra dịch vụ SQL Server đã chạy, tên máy và tên instance trong chuỗi kết nối (xem README).");
        }
        catch (Exception ex)
        {
            return DatabaseStatus.Loi($"Lỗi kết nối SQL Server: {ex.GetBaseException().Message}");
        }
    }
}
