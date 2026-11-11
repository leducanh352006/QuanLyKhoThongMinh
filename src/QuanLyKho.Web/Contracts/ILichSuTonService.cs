namespace QuanLyKho.Web.Contracts;

/// <summary>
/// Lịch sử biến động tồn do Nhóm 1 (TV4) sở hữu, đọc từ sổ cái BienDongTon mà <see cref="IStockService"/> ghi (việc T4-6).
/// Chỉ đọc, không đổi tồn. Ban đầu dành cho G3, G4 (đã bỏ); giữ lại làm dịch vụ tra cứu lịch sử xuất.
/// </summary>
public interface ILichSuTonService
{
    /// <summary>
    /// Lượng hàng đã xuất theo từng ngày, biến thể và kho, từ <paramref name="tuNgay"/> đến <paramref name="denNgay"/> (tính cả hai đầu).
    /// <para>"Xuất" là dòng sổ cái loại <see cref="LoaiBienDong.XuatKho"/> (hàng ra khỏi kho để bán hoặc tiêu hủy);
    /// chuyển kho (<see cref="LoaiBienDong.ChuyenDi"/>) và đổi ô không tính vì không phải nhu cầu của khách.</para>
    /// <para>Ngày tính theo giờ máy chủ (sổ cái lưu giờ UTC). Số lượng trả về luôn dương; chỉ trả những ngày có xuất.
    /// <paramref name="maKho"/> và <paramref name="maBT"/> là tùy chọn, null là tất cả.</para>
    /// </summary>
    Task<IReadOnlyList<XuatTheoNgayDto>> LichSuXuatTheoNgay(
        DateOnly tuNgay, DateOnly denNgay, int? maKho = null, int? maBT = null, CancellationToken ct = default);
}
