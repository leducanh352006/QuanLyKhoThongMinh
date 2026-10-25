namespace QuanLyKho.Web.Infrastructure.Database;

public enum TrangThaiKetNoi { ChuaCauHinh, DaKetNoi, Loi }

public sealed record DatabaseStatus(TrangThaiKetNoi TrangThai, string ThongBao)
{
    public static DatabaseStatus ChuaCauHinh() =>
        new(TrangThaiKetNoi.ChuaCauHinh, "Chưa cấu hình chuỗi kết nối ConnectionStrings:KhoDb (xem README, bước 3).");

    public static DatabaseStatus DaKetNoi() => new(TrangThaiKetNoi.DaKetNoi, "Kết nối SQL Server thành công.");

    public static DatabaseStatus Loi(string thongBao) => new(TrangThaiKetNoi.Loi, thongBao);
}

/// <summary>Cho trang chủ biết đã nối được SQL Server chưa. Ứng dụng vẫn mở được khi chưa có CSDL.</summary>
public interface IDatabaseStatusService
{
    Task<DatabaseStatus> KiemTraAsync(CancellationToken ct = default);
}
