using QuanLyKho.Web.Areas.HeThong.Models;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>
/// Cổng truy cập dữ liệu Kho, ViTri (việc T4-3), cùng kiểu với <see cref="IHeThongStore"/>: có bản EF Core (SQL Server) và
/// bản trong bộ nhớ (chỉ Development, chưa có chuỗi kết nối). Đối tượng trả về được theo dõi: sửa thuộc tính rồi gọi <see cref="LuuThayDoi"/>.
/// </summary>
public interface IKhoViTriStore
{
    Task<IReadOnlyList<Kho>> LayKho(CancellationToken ct = default);

    Task<Kho?> TimKho(int maKho, CancellationToken ct = default);

    /// <exception cref="XungDotDuLieuException">Trùng tên kho.</exception>
    Task<Kho> ThemKho(Kho kho, CancellationToken ct = default);

    Task<IReadOnlyList<ViTri>> LayViTriTheoKho(int maKho, CancellationToken ct = default);

    Task<ViTri?> TimViTri(int maViTri, CancellationToken ct = default);

    /// <exception cref="XungDotDuLieuException">Trùng tên trong cùng vị trí cha, hoặc trùng mã vạch ô.</exception>
    Task<ViTri> ThemViTri(ViTri viTri, CancellationToken ct = default);

    /// <summary>Số ô loại Tiếp nhận đang hoạt động của một kho, không tính <paramref name="botMaViTri"/> (để biết còn ô nào khác không).</summary>
    Task<int> DemOTiepNhanHoatDong(int maKho, int? botMaViTri, CancellationToken ct = default);

    Task<int> DemConHoatDong(int maViTriCha, CancellationToken ct = default);

    Task LuuThayDoi(CancellationToken ct = default);

    /// <summary>Xóa cứng một vị trí.</summary>
    /// <exception cref="XungDotDuLieuException">Vị trí còn dữ liệu khác tham chiếu tới (sổ cái, tồn, phiếu...).</exception>
    Task XoaViTri(ViTri viTri, CancellationToken ct = default);
}
