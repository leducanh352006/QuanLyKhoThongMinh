using QuanLyKho.Web.Areas.HangHoa.Models;

namespace QuanLyKho.Web.Areas.HangHoa.Services;

/// <summary>Ô tìm được khi quét mã ô.</summary>
public sealed record OQuet(int MaViTri, string TenViTri, int MaKho, bool LaOLuuTru);

/// <summary>Lô tìm được khi quét mã lô.</summary>
public sealed record LoQuet(int MaLo, string MaVachLo, int MaBT);

/// <summary>Ô để chọn làm ô đúng khi ghi nhận phát sinh sai vị trí (việc T3-6), kèm kích thước lòng ô để kiểm lọt lòng.</summary>
public sealed record ViTriChon(
    int MaViTri, string TenViTri, int MaKho, bool LaOLuuTru, bool DangHoatDong, bool DangKhoa,
    decimal? Dai = null, decimal? Rong = null, decimal? Cao = null);

/// <summary>Kích thước một đơn vị biến thể (cm).</summary>
public sealed record KichThuocBienThe(decimal Dai, decimal Rong, decimal Cao);

/// <summary>
/// Cổng truy cập dữ liệu của bước kiểm đếm (việc T3-5) và ghi nhận phát sinh (việc T3-6), cùng kiểu với <see cref="IPhieuKiemStore"/>
/// để kiểm thử nghiệp vụ mà không cần SQL Server.
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

    // ---- Ghi nhận phát sinh (T3-6) ----

    /// <summary>Ô theo mã vị trí; null nếu không có.</summary>
    Task<ViTriChon?> TimViTri(int maViTri, CancellationToken ct = default);

    /// <summary>Các ô Lưu trữ đang hoạt động của kho, để chọn ô đúng.</summary>
    Task<IReadOnlyList<ViTriChon>> LayOLuuTruHoatDong(int maKho, CancellationToken ct = default);

    /// <summary>Tên hiển thị của biến thể (sản phẩm và thuộc tính); null nếu không có biến thể này.</summary>
    Task<string?> TenBienThe(int maBT, CancellationToken ct = default);

    /// <summary>Kích thước một đơn vị biến thể; null nếu không có biến thể này.</summary>
    Task<KichThuocBienThe?> KichThuoc(int maBT, CancellationToken ct = default);

    /// <summary>Danh sách biến thể để chọn mã hàng đúng.</summary>
    Task<IReadOnlyList<KeyValuePair<int, string>>> LayBienTheChon(CancellationToken ct = default);
}
