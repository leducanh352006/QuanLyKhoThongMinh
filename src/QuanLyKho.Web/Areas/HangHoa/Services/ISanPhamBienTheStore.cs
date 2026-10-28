using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Services;

namespace QuanLyKho.Web.Areas.HangHoa.Services;

/// <summary>
/// Cổng truy cập dữ liệu SanPham, BienThe (việc T3-2), cùng kiểu với <see cref="IHangHoaStore"/>: có bản EF Core (SQL Server)
/// và bản trong bộ nhớ (chỉ Development, chưa có chuỗi kết nối). Đối tượng trả về được theo dõi: sửa thuộc tính rồi gọi <see cref="LuuThayDoi"/>.
/// </summary>
public interface ISanPhamBienTheStore
{
    Task<IReadOnlyList<SanPham>> LaySanPham(CancellationToken ct = default);

    Task<SanPham?> TimSanPham(int maSP, CancellationToken ct = default);

    Task<SanPham> ThemSanPham(SanPham sp, CancellationToken ct = default);

    Task<IReadOnlyList<BienThe>> LayBienTheTheoSanPham(int maSP, CancellationToken ct = default);

    Task<BienThe?> TimBienThe(int maBT, CancellationToken ct = default);

    /// <exception cref="XungDotDuLieuException">Trùng mã vạch.</exception>
    Task<BienThe> ThemBienThe(BienThe bt, CancellationToken ct = default);

    Task LuuThayDoi(CancellationToken ct = default);
}
