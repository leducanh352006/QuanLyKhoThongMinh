namespace QuanLyKho.Web.Contracts;

/// <summary>
/// Khung gợi ý AI dùng chung (GoiYAI, ChiTietGoiY) do Nhóm 1 sở hữu. AI chỉ đề xuất; gợi ý chỉ có hiệu lực sau khi được duyệt (mục 1.2.4).
/// Mỗi nhóm gắn bài toán của mình bằng cách đăng ký một <see cref="IGoiYHandler"/>: nhóm 3 cho G1, nhóm 2 cho G2, nhóm 5 cho G3 và G4.
/// </summary>
public interface IGoiYService
{
    /// <summary>Lưu một gợi ý ở trạng thái Chờ duyệt. G1 bắt buộc MaPX; G2 bắt buộc MaPN; G3 và G4 chưa có phiếu (điền sau khi duyệt).</summary>
    Task<KetQua<int>> Tao(LoaiGoiY loai, ThamChieuGoiY thamChieu, IReadOnlyList<ChiTietGoiYDto> chiTiet, CancellationToken ct = default);

    Task<GoiYDto?> Lay(int maGoiY, CancellationToken ct = default);

    Task<IReadOnlyList<GoiYDto>> DanhSachChoDuyet(LoaiGoiY? loai = null, CancellationToken ct = default);

    /// <summary>Duyệt nguyên đề xuất. Gọi <see cref="IGoiYHandler"/> của loại gợi ý; chỉ khi handler thành công mới đổi sang Đã duyệt.</summary>
    Task<KetQua> Duyet(int maGoiY, int maNguoiDuyet, CancellationToken ct = default);

    /// <summary>Người duyệt sửa lại các dòng rồi duyệt: lưu trạng thái Đã sửa và duyệt; dòng gốc của AI vẫn giữ nguyên để so sánh.</summary>
    Task<KetQua> SuaVaDuyet(int maGoiY, IReadOnlyList<ChiTietGoiYDto> chiTietDaSua, int maNguoiDuyet, CancellationToken ct = default);

    Task<KetQua> TuChoi(int maGoiY, int maNguoiDuyet, string? lyDo = null, CancellationToken ct = default);
}

/// <summary>Điểm gắn của nhóm sở hữu từng bài toán: sau khi gợi ý được duyệt, khung gọi handler để tạo thao tác thay người dùng.</summary>
public interface IGoiYHandler
{
    LoaiGoiY Loai { get; }

    /// <summary>
    /// Thực hiện thao tác sau khi duyệt (ví dụ G3 tạo phiếu nhập nháp, G4 tạo phiếu chuyển). Trả tham chiếu phiếu được tạo hoặc cập nhật.
    /// Nếu thất bại, gợi ý vẫn ở Chờ duyệt và người dùng làm tay được (mục 1.2.4).
    /// Thao tác chạm hàng thật (lấy, xếp, nhận) vẫn do thủ kho quét mã xác nhận, handler không tự thực hiện.
    /// </summary>
    Task<KetQua<ThamChieuGoiY>> ThucHien(GoiYDto goiY, int maNguoiDuyet, CancellationToken ct = default);
}
