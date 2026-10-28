using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Services;

namespace QuanLyKho.Web.Areas.HangHoa.Services;

/// <summary>
/// Cổng truy cập dữ liệu NhaCungCap, DanhMuc (việc T3-1), cùng kiểu với <see cref="IKhoViTriStore"/>: có bản EF Core (SQL Server)
/// và bản trong bộ nhớ (chỉ Development, chưa có chuỗi kết nối). Đối tượng trả về được theo dõi: sửa thuộc tính rồi gọi <see cref="LuuThayDoi"/>.
/// </summary>
public interface IHangHoaStore
{
    Task<IReadOnlyList<NhaCungCap>> LayNhaCungCap(CancellationToken ct = default);

    Task<NhaCungCap?> TimNhaCungCap(int maNCC, CancellationToken ct = default);

    /// <exception cref="XungDotDuLieuException">Trùng tên nhà cung cấp.</exception>
    Task<NhaCungCap> ThemNhaCungCap(NhaCungCap ncc, CancellationToken ct = default);

    Task<IReadOnlyList<DanhMuc>> LayDanhMuc(CancellationToken ct = default);

    Task<DanhMuc?> TimDanhMuc(int maDM, CancellationToken ct = default);

    /// <exception cref="XungDotDuLieuException">Trùng tên danh mục.</exception>
    Task<DanhMuc> ThemDanhMuc(DanhMuc danhMuc, CancellationToken ct = default);

    /// <exception cref="XungDotDuLieuException">Danh mục còn sản phẩm tham chiếu, không xóa được.</exception>
    Task XoaDanhMuc(DanhMuc danhMuc, CancellationToken ct = default);

    Task LuuThayDoi(CancellationToken ct = default);
}
