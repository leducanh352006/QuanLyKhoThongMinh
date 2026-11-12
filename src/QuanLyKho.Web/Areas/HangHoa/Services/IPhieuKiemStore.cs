using QuanLyKho.Web.Areas.HangHoa.Models;

namespace QuanLyKho.Web.Areas.HangHoa.Services;

/// <summary>
/// Cổng truy cập dữ liệu của phiếu kiểm (việc T3-4). Tách khỏi <see cref="PhieuKiemService"/> để kiểm thử nghiệp vụ tạo phiếu
/// (thứ tự khóa ô, chụp tồn, mở lại ô khi lỗi) mà không cần SQL Server. Số tồn KHÔNG đọc ở đây: luôn lấy qua IStockService.
/// </summary>
public interface IPhieuKiemStore
{
    /// <summary>Kho có tồn tại và đang hoạt động không.</summary>
    Task<bool> KhoHoatDong(int maKho, CancellationToken ct = default);

    /// <summary>Mã các ô loại Lưu trữ của kho (không gồm khu Tiếp nhận), kể cả ô đang trống.</summary>
    Task<IReadOnlyList<int>> LayOLuuTru(int maKho, CancellationToken ct = default);

    /// <summary>Số người dùng đang hoạt động trong danh sách mã đã cho.</summary>
    Task<int> DemNguoiHoatDong(IReadOnlyCollection<int> maND, CancellationToken ct = default);

    /// <summary>Số biến thể có thật trong danh sách mã đã cho.</summary>
    Task<int> DemBienThe(IReadOnlyCollection<int> maBT, CancellationToken ct = default);

    /// <summary>Biến thể (MaBT) của từng lô.</summary>
    Task<IReadOnlyDictionary<int, int>> LayBienTheCuaLo(IReadOnlyCollection<int> maLo, CancellationToken ct = default);

    /// <summary>
    /// Ghi phiếu cùng dòng chi tiết và người tham gia trong một giao dịch, điền <see cref="PhieuKiem.MaPK"/> và số phiếu KK.
    /// Lỗi thì ném ngoại lệ và không để lại gì trong CSDL lẫn bộ theo dõi thay đổi.
    /// </summary>
    Task Luu(PhieuKiem phieu, CancellationToken ct = default);

    /// <summary>Các phiếu mới nhất, tối đa <paramref name="toiDa"/> phiếu.</summary>
    Task<IReadOnlyList<PhieuKiemDong>> DanhSach(int toiDa, CancellationToken ct = default);

    Task<PhieuKiemXem?> Xem(int maPK, CancellationToken ct = default);

    /// <summary>Danh mục để chọn trên form tạo phiếu.</summary>
    Task<DanhMucChonPhieuKiem> LayDanhMucChon(CancellationToken ct = default);
}
