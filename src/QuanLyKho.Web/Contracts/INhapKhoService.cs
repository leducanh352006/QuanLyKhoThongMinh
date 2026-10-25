namespace QuanLyKho.Web.Contracts;

/// <summary>Hàm dùng chung do Nhóm 2 (TV1) sở hữu, cho nhóm 4 (phiếu Thừa) và nhóm 5 (phiếu nháp từ G3) gọi (Bảng 1.6 dòng 10 và 11).</summary>
public interface INhapKhoService
{
    /// <summary>
    /// Tạo phiếu nhập loại "Thừa do kiểm kê" từ phiếu kiểm <paramref name="maPK"/>. Phiếu tự hoàn tất khi quản lý kho duyệt, không qua kiểm tra và sắp xếp.
    /// Mỗi dòng phải có MaLo và MaViTri (nơi hàng thừa đang nằm).
    /// </summary>
    Task<KetQua<PhieuRef>> TaoPhieuNhapThua(int maKho, int maPK, IReadOnlyList<DongPhieu> dong, int maNguoiLap, CancellationToken ct = default);

    /// <summary>
    /// Tạo phiếu nhập ở trạng thái Nháp (tên phương thức giữ đúng theo issue #41: "nhập nháp"), dùng cho gợi ý G3.
    /// Chưa có nhà cung cấp; quản lý kho chọn khi duyệt, rồi phiếu vẫn phải qua bước kiểm tra.
    /// </summary>
    Task<KetQua<PhieuRef>> TaoPhieuNhapNhap(int maKho, IReadOnlyList<DongPhieu> dong, int maNguoiLap, int? maGoiY = null, CancellationToken ct = default);
}
