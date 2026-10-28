using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Services;

namespace QuanLyKho.Web.Areas.HangHoa.Services;

/// <summary>
/// Cổng truy cập dữ liệu LoHang (việc T3-3), cùng kiểu với <see cref="ISanPhamBienTheStore"/>: có bản EF Core (SQL Server)
/// và bản trong bộ nhớ (chỉ Development, chưa có chuỗi kết nối).
/// </summary>
public interface ILoHangStore
{
    Task<IReadOnlyList<LoHang>> LayTheoBienThe(int maBT, CancellationToken ct = default);

    Task<LoHang?> Tim(int maLo, CancellationToken ct = default);

    Task<LoHang?> TimTheoMaVach(string maVachLo, CancellationToken ct = default);

    /// <exception cref="XungDotDuLieuException">Trùng mã vạch lô.</exception>
    Task<LoHang> Them(LoHang lo, CancellationToken ct = default);
}
