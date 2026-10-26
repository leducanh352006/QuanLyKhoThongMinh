namespace QuanLyKho.Web.Contracts;

/// <summary>Hàm dùng chung do Nhóm 3 (TV2) sở hữu, cho nhóm 4 gọi (Bảng 1.6 dòng 12).</summary>
public interface IXuatKhoService
{
    /// <summary>
    /// Tạo phiếu xuất loại "Thiếu do kiểm kê" từ phiếu kiểm <paramref name="maPK"/>. Phiếu tự hoàn tất khi được duyệt, không qua lấy hàng.
    /// Mỗi dòng phải có MaLo và MaViTri (nơi hàng bị thiếu).
    /// </summary>
    Task<KetQua<PhieuRef>> TaoPhieuXuatThieu(int maKho, int maPK, IReadOnlyList<DongPhieu> dong, int maNguoiLap, CancellationToken ct = default);
}
