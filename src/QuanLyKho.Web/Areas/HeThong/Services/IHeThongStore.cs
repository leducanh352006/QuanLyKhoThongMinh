using QuanLyKho.Web.Areas.HeThong.Models;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>Ghi nhận vi phạm ràng buộc duy nhất khi lưu (hai người cùng thêm một tên đăng nhập).</summary>
public sealed class XungDotDuLieuException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>
/// Cổng truy cập dữ liệu của Area HeThong. Có hai bản: <see cref="EfHeThongStore"/> (SQL Server qua EF Core) và
/// <see cref="BoNhoHeThongStore"/> (trong bộ nhớ, chỉ môi trường Development khi chưa cấu hình chuỗi kết nối và dùng cho kiểm thử).
/// Các dịch vụ nghiệp vụ chỉ biết interface này nên kiểm thử được mà không cần SQL Server.
/// <para>Đối tượng trả về của <see cref="TimNguoiDung"/>, <see cref="TimNguoiDungTheoTen"/> được theo dõi: sửa thuộc tính rồi gọi <see cref="LuuThayDoi"/>.</para>
/// </summary>
public interface IHeThongStore
{
    Task<IReadOnlyList<VaiTro>> LayVaiTro(CancellationToken ct = default);

    Task<IReadOnlyList<NguoiDung>> LayNguoiDung(CancellationToken ct = default);

    Task<NguoiDung?> TimNguoiDung(int maND, CancellationToken ct = default);

    /// <summary>Không phân biệt hoa thường (khớp collation mặc định của SQL Server).</summary>
    Task<NguoiDung?> TimNguoiDungTheoTen(string tenDangNhap, CancellationToken ct = default);

    /// <exception cref="XungDotDuLieuException">Trùng tên đăng nhập.</exception>
    Task<NguoiDung> ThemNguoiDung(NguoiDung nguoiDung, CancellationToken ct = default);

    Task LuuThayDoi(CancellationToken ct = default);

    /// <summary>Số Quản trị viên đang hoạt động, không tính <paramref name="botMaND"/> (để biết còn ai khác không).</summary>
    Task<int> DemQuanTriVienHoatDong(int? botMaND, CancellationToken ct = default);

    Task<IReadOnlyList<PhanQuyen>> LayPhanQuyen(CancellationToken ct = default);

    /// <summary>Ghi đè các dòng quyền của một vai trò (thêm dòng chưa có, sửa dòng đã có) trong một giao dịch.</summary>
    Task ThayPhanQuyenCuaVaiTro(int maVaiTro, IReadOnlyList<PhanQuyen> dong, CancellationToken ct = default);
}
