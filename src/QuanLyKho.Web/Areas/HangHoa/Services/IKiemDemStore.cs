using QuanLyKho.Web.Areas.HangHoa.Models;

namespace QuanLyKho.Web.Areas.HangHoa.Services;

/// <summary>Ô tìm được khi quét mã ô.</summary>
public sealed record OQuet(int MaViTri, string TenViTri, int MaKho, bool LaOLuuTru);

/// <summary>Lô tìm được khi quét mã lô.</summary>
public sealed record LoQuet(int MaLo, string MaVachLo, int MaBT);

/// <summary>
/// Cổng truy cập dữ liệu của bước kiểm đếm (việc T3-5), cùng kiểu với <see cref="IPhieuKiemStore"/> để kiểm thử nghiệp vụ
/// quét mã và tính chênh lệch mà không cần SQL Server.
/// </summary>
public interface IKiemDemStore
{
    Task<PhieuKiemTomTat?> LayPhieu(int maPK, CancellationToken ct = default);

    /// <summary>Mọi dòng kiểm của phiếu, xếp theo ô rồi tên hàng, mã lô.</summary>
    Task<IReadOnlyList<DongKiemXem>> LayDong(int maPK, CancellationToken ct = default);

    Task<OQuet?> TimO(string maVachO, CancellationToken ct = default);

    Task<LoQuet?> TimLo(string maVachLo, CancellationToken ct = default);

    /// <summary>Mã biến thể theo mã vạch hàng; null nếu không có.</summary>
    Task<int?> TimBienThe(string maVach, CancellationToken ct = default);

    /// <summary>Dòng kiểm (đang được theo dõi để sửa) theo mã dòng trong một phiếu.</summary>
    Task<DongKiem?> TimDong(int maPK, int maDK, CancellationToken ct = default);

    void Them(DongKiem dong);

    void Xoa(DongKiem dong);

    /// <exception cref="InvalidOperationException">Trùng dòng (cùng phiếu, ô, lô) hoặc lỗi ghi; không để lại thay đổi dở trong bộ theo dõi.</exception>
    Task LuuThayDoi(CancellationToken ct = default);
}
