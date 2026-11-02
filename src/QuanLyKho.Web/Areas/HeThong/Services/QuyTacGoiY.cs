using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>Quy tắc của khung gợi ý AI không cần CSDL: kiểm dữ liệu, ai được duyệt. Dùng chung bản thật và kiểm thử trực tiếp.</summary>
public static class QuyTacGoiY
{
    public const int DoDaiToiDaLyDo = 500;

    /// <summary>
    /// Ai được duyệt, sửa rồi duyệt hoặc từ chối từng loại gợi ý (báo cáo mục 2.3: G1 thủ kho lấy hàng và quản lý kho duyệt cùng phiếu xuất;
    /// G2 thủ kho xếp hàng; G3, G4 quản lý kho). Quản trị viên được xử lý mọi loại. Muốn đổi thẩm quyền chỉ sửa bảng này.
    /// </summary>
    private static readonly IReadOnlyDictionary<LoaiGoiY, string[]> VaiTroTheoLoai = new Dictionary<LoaiGoiY, string[]>
    {
        [LoaiGoiY.G1] = new[] { VaiTroTen.QuanLyKho, VaiTroTen.QuanTriVien },
        [LoaiGoiY.G2] = new[] { VaiTroTen.ThuKho, VaiTroTen.QuanTriVien },
        [LoaiGoiY.G3] = new[] { VaiTroTen.QuanLyKho, VaiTroTen.QuanTriVien },
        [LoaiGoiY.G4] = new[] { VaiTroTen.QuanLyKho, VaiTroTen.QuanTriVien },
    };

    public static IReadOnlyList<string> VaiTroDuocDuyet(LoaiGoiY loai) => VaiTroTheoLoai[loai];

    public static bool DuocDuyet(LoaiGoiY loai, string? tenVaiTro) => tenVaiTro is not null && VaiTroTheoLoai[loai].Contains(tenVaiTro);

    /// <summary>Loại gợi ý mà một vai trò được xử lý (để lọc danh sách); vai trò không có quyền nào trả rỗng.</summary>
    public static IReadOnlyList<LoaiGoiY> LoaiDuocDuyet(string? tenVaiTro) =>
        Enum.GetValues<LoaiGoiY>().Where(l => DuocDuyet(l, tenVaiTro)).ToList();

    /// <summary>G1 gắn đúng một phiếu xuất, G2 đúng một phiếu nhập, G3 và G4 chưa có phiếu khi tạo (phiếu điền sau khi duyệt).</summary>
    public static string? KiemTraThamChieuKhiTao(LoaiGoiY loai, ThamChieuGoiY t) => loai switch
    {
        LoaiGoiY.G1 when t.MaPX is null || t.MaPN is not null || t.MaPC is not null => "G1 phải gắn đúng một phiếu xuất (MaPX).",
        LoaiGoiY.G2 when t.MaPN is null || t.MaPX is not null || t.MaPC is not null => "G2 phải gắn đúng một phiếu nhập (MaPN).",
        LoaiGoiY.G3 or LoaiGoiY.G4 when t.MaPX is not null || t.MaPN is not null || t.MaPC is not null => $"{loai} chưa có phiếu khi tạo; phiếu được điền sau khi duyệt.",
        _ => LoaiHopLe(loai) ? null : "Loại gợi ý không hợp lệ.",
    };

    /// <summary>Tham chiếu sau khi duyệt: G1 chỉ phiếu xuất, G2 chỉ phiếu nhập, G3 chỉ phiếu nhập (nếu có), G4 chỉ phiếu chuyển (nếu có).</summary>
    public static string? KiemTraThamChieuSauDuyet(LoaiGoiY loai, ThamChieuGoiY t) => loai switch
    {
        LoaiGoiY.G1 when t.MaPX is null || t.MaPN is not null || t.MaPC is not null => "Thao tác của G1 chỉ được gắn phiếu xuất.",
        LoaiGoiY.G2 when t.MaPN is null || t.MaPX is not null || t.MaPC is not null => "Thao tác của G2 chỉ được gắn phiếu nhập.",
        LoaiGoiY.G3 when t.MaPX is not null || t.MaPC is not null => "Thao tác của G3 chỉ được gắn phiếu nhập.",
        LoaiGoiY.G4 when t.MaPX is not null || t.MaPN is not null => "Thao tác của G4 chỉ được gắn phiếu chuyển.",
        _ => null,
    };

    public static string? KiemTraChiTiet(IReadOnlyList<ChiTietGoiYDto> chiTiet)
    {
        if (chiTiet.Count == 0) return "Gợi ý phải có ít nhất một dòng đề xuất.";
        foreach (var c in chiTiet)
        {
            if (c.MaBT <= 0) return "Mỗi dòng đề xuất phải có biến thể.";
            if (c.SoLuong <= 0) return "Số lượng đề xuất phải lớn hơn 0.";
            if (string.IsNullOrWhiteSpace(c.LyDo)) return "Mỗi dòng đề xuất phải kèm lý do và số liệu căn cứ.";
            if (c.LyDo.Length > DoDaiToiDaLyDo) return $"Lý do tối đa {DoDaiToiDaLyDo} ký tự.";
            if (c.MaKhoNguon.HasValue && c.MaKhoDich.HasValue && c.MaKhoNguon == c.MaKhoDich) return "Kho nguồn và kho đích phải khác nhau.";
        }
        return null;
    }

    public static bool LoaiHopLe(LoaiGoiY loai) => Enum.IsDefined(loai);

    /// <summary>Tên bài toán hiển thị (Bảng 1.3): G1 lấy hàng, G2 xếp hàng, G3 nhập bổ sung, G4 luân chuyển kho.</summary>
    public static string Ten(LoaiGoiY loai) => loai switch
    {
        LoaiGoiY.G1 => "Lấy hàng từ lô, ô nào",
        LoaiGoiY.G2 => "Xếp hàng nhập vào ô nào",
        LoaiGoiY.G3 => "Nhập bổ sung mặt hàng nào, bao nhiêu",
        LoaiGoiY.G4 => "Luân chuyển hàng giữa các kho",
        _ => loai.ToString(),
    };
}
