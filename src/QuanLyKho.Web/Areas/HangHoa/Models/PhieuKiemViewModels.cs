namespace QuanLyKho.Web.Areas.HangHoa.Models;

/// <summary>
/// Dữ liệu form tạo phiếu kiểm (việc T3-4). Chỉ gồm các ô người dùng được nhập: số phiếu, trạng thái, người lập và thời gian
/// bắt đầu do máy chủ tự điền, nên không bind thẳng entity <see cref="PhieuKiem"/> (chống gửi thừa trường).
/// </summary>
public class TaoPhieuKiemVm
{
    public int MaKho { get; set; }

    public string? LoaiKiemKe { get; set; } = PhieuKiem.ToanBo;

    public string? ThamChieu { get; set; }

    public string? DienGiai { get; set; }

    /// <summary>Phạm vi mặt hàng (MaBT) khi kiểm Một phần; không lưu cột riêng, chỉ dùng để xác định ô và dòng kiểm lúc tạo.</summary>
    public List<int>? PhamViMaBT { get; set; } = new();

    public List<ThamGiaVm?>? ThamGia { get; set; } = new();

    /// <summary>
    /// Chuẩn hóa (bỏ phần tử rỗng, cắt khoảng trắng) rồi kiểm các quy tắc không cần CSDL (Bảng 2.4 dòng 1 và 2).
    /// Trả về lỗi, hoặc null nếu hợp lệ. Gọi xong thì <see cref="PhamViMaBT"/> và <see cref="ThamGia"/> không còn null.
    /// </summary>
    public string? KiemTra()
    {
        LoaiKiemKe = LoaiKiemKe?.Trim();
        ThamChieu = string.IsNullOrWhiteSpace(ThamChieu) ? null : ThamChieu.Trim();
        DienGiai = string.IsNullOrWhiteSpace(DienGiai) ? null : DienGiai.Trim();
        PhamViMaBT = (PhamViMaBT ?? new()).Distinct().ToList();
        ThamGia = (ThamGia ?? new()).Where(t => t is not null).ToList();
        foreach (var t in ThamGia) t!.GhiChu = string.IsNullOrWhiteSpace(t.GhiChu) ? null : t.GhiChu.Trim();

        if (LoaiKiemKe is not (PhieuKiem.ToanBo or PhieuKiem.MotPhan)) return "Loại kiểm kê chỉ được chọn Toàn bộ hoặc Một phần.";
        if (MaKho <= 0) return "Chọn kho kiểm kê.";
        if (LoaiKiemKe == PhieuKiem.MotPhan && PhamViMaBT.Count == 0) return "Kiểm kê một phần phải chọn phạm vi mặt hàng.";
        if (LoaiKiemKe == PhieuKiem.ToanBo && PhamViMaBT.Count > 0) return "Kiểm kê toàn bộ không chọn phạm vi mặt hàng.";
        if (PhamViMaBT.Any(m => m <= 0)) return "Có mặt hàng trong phạm vi kiểm không hợp lệ.";
        if (ThamGia.Count == 0) return "Phiếu kiểm phải khai báo ít nhất một người tham gia kiểm.";
        if (ThamGia.Any(t => t!.MaND <= 0)) return "Chọn người cho mỗi dòng tham gia kiểm.";
        if (ThamGia.Select(t => t!.MaND).Distinct().Count() != ThamGia.Count) return "Một người không khai báo tham gia hai lần trên cùng phiếu.";
        if (ThamGia.Any(t => t!.GhiChu?.Length > 255)) return "Ghi chú của người kiểm tối đa 255 ký tự.";
        if (ThamChieu?.Length > 100) return "Tham chiếu tối đa 100 ký tự.";
        if (DienGiai?.Length > 500) return "Diễn giải tối đa 500 ký tự.";
        return null;
    }
}

/// <summary>Một dòng người tham gia kiểm trên form.</summary>
public class ThamGiaVm
{
    public int MaND { get; set; }

    public string? GhiChu { get; set; }
}

/// <summary>Danh mục để chọn trên form tạo phiếu: kho đang hoạt động, mặt hàng, nhân viên đang hoạt động.</summary>
public sealed record DanhMucChonPhieuKiem(
    IReadOnlyList<KeyValuePair<int, string>> Kho,
    IReadOnlyList<KeyValuePair<int, string>> BienThe,
    IReadOnlyList<KeyValuePair<int, string>> NguoiDung);

/// <summary>Dữ liệu màn hình tạo phiếu: form đang nhập kèm danh mục để chọn.</summary>
public sealed record TaoPhieuKiemTrang(TaoPhieuKiemVm Form, DanhMucChonPhieuKiem DanhMuc);

/// <summary>Một dòng trên danh sách phiếu kiểm.</summary>
public sealed record PhieuKiemDong(
    int MaPK, string SoPhieu, string LoaiKiemKe, DateTime ThoiGianBatDau, int MaKho, string TenKho,
    string NguoiLap, int SoDong, int SoNguoi, string TrangThai);

/// <summary>Người tham gia kiểm khi xem phiếu (Bảng 2.4 dòng 2: mã nhân viên, họ tên, chức danh, ghi chú).</summary>
public sealed record ThamGiaXem(int MaND, string HoTen, string ChucDanh, string? GhiChu);

/// <summary>Một dòng số hệ thống khi xem phiếu: tên hàng, mã lô và tên ô thay cho các mã số nội bộ.</summary>
public sealed record DongHeThongXem(string TenHang, string MaVachLo, string TenO, int SoLuongHeThong);

/// <summary>Dữ liệu màn hình xem một phiếu kiểm đã tạo.</summary>
public sealed record PhieuKiemXem(
    int MaPK, string SoPhieu, int MaKho, string TenKho, string LoaiKiemKe, string? ThamChieu, string? DienGiai,
    DateTime ThoiGianBatDau, string NguoiLap, string TrangThai,
    IReadOnlyList<ThamGiaXem> ThamGia, IReadOnlyList<DongHeThongXem> ChiTiet);

/// <summary>Lớp huy hiệu <c>.nhan</c> (site.css) theo trạng thái phiếu kiểm.</summary>
public static class PhieuKiemHienThi
{
    public static string LopNhan(string trangThai) => trangThai switch
    {
        PhieuKiem.DangKiem => "nhan-lam",
        PhieuKiem.ChoDuyet => "nhan-chu-y",
        PhieuKiem.DaXuLy => "nhan-tot",
        _ => "nhan-xam",
    };
}
