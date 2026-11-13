namespace QuanLyKho.Web.Areas.HangHoa.Models;

/// <summary>Dữ liệu một lần ghi số đếm (việc T3-5): quét mã ô, quét mã lô hoặc mã vạch hàng, nhập số lượng thực tế.</summary>
public class GhiNhanDemVm
{
    /// <summary>Mã vạch ô (ViTri.MaVachO).</summary>
    public string? MaVachO { get; set; }

    /// <summary>Mã lô (LoHang.MaVachLo) hoặc mã vạch hàng (BienThe.MaVach).</summary>
    public string? MaHang { get; set; }

    public int? SLThucTe { get; set; }

    /// <summary>Xác nhận hàng nằm ở ô này dù số hệ thống không có (hàng ngoài sổ): thêm dòng mới với số hệ thống 0.</summary>
    public bool NgoaiSo { get; set; }
}

/// <summary>Bốn tab tự chia theo số đếm (Bảng 2.4): Khớp, Thừa, Thiếu, Chưa kiểm.</summary>
public enum TabKiemDem { TatCa, Khop, Thua, Thieu, ChuaKiem }

/// <summary>Thông tin phiếu trên màn hình kiểm đếm.</summary>
public sealed record PhieuKiemTomTat(int MaPK, string SoPhieu, int MaKho, string TenKho, string LoaiKiemKe, string TrangThai)
{
    public bool DangKiem => TrangThai == PhieuKiem.DangKiem;
}

/// <summary>Một dòng kiểm đếm để hiển thị, kèm tên ô, mã lô, tên hàng thay cho mã số nội bộ.</summary>
public sealed record DongKiemXem(
    int MaDK, int MaViTri, string TenO, string? MaVachO, int MaLo, string MaVachLo, int MaBT, string TenHang, string MaVachHang,
    int SLHeThong, int? SLThucTe, string TrangThaiXuLy, string? LoaiPhatSinh, string? NguoiDem)
{
    /// <summary>Chênh lệch = tồn thực tế trừ tồn kho; null khi chưa kiểm.</summary>
    public int? ChenhLech => SLThucTe - SLHeThong;

    public TabKiemDem Tab => ChenhLech switch
    {
        null => TabKiemDem.ChuaKiem,
        0 => TabKiemDem.Khop,
        > 0 => TabKiemDem.Thua,
        _ => TabKiemDem.Thieu,
    };

    public bool DaChot => TrangThaiXuLy is DongKiem.DaDuyet or DongKiem.TuChoi;
}

/// <summary>Dữ liệu màn hình kiểm đếm: phiếu, mọi dòng và tab đang xem.</summary>
public sealed record KiemDemTrang(PhieuKiemTomTat Phieu, IReadOnlyList<DongKiemXem> Dong, TabKiemDem Tab, GhiNhanDemVm Form)
{
    public int Dem(TabKiemDem tab) => tab == TabKiemDem.TatCa ? Dong.Count : Dong.Count(d => d.Tab == tab);

    public IEnumerable<DongKiemXem> DongCuaTab => Tab == TabKiemDem.TatCa ? Dong : Dong.Where(d => d.Tab == Tab);
}

/// <summary>Chữ và lớp huy hiệu <c>.nhan</c> (site.css) cho tab và trạng thái dòng kiểm.</summary>
public static class KiemDemHienThi
{
    public static string Ten(TabKiemDem tab) => tab switch
    {
        TabKiemDem.Khop => "Khớp",
        TabKiemDem.Thua => "Thừa",
        TabKiemDem.Thieu => "Thiếu",
        TabKiemDem.ChuaKiem => "Chưa kiểm",
        _ => "Tất cả",
    };

    public static string LopNhan(TabKiemDem tab) => tab switch
    {
        TabKiemDem.Khop => "nhan-tot",
        TabKiemDem.Thua => "nhan-lam",
        TabKiemDem.Thieu => "nhan-loi",
        TabKiemDem.ChuaKiem => "nhan-xam",
        _ => "nhan-xam",
    };

    public static string LopNhanTrangThai(string trangThaiXuLy) => trangThaiXuLy switch
    {
        DongKiem.Khop or DongKiem.DaDuyet => "nhan-tot",
        DongKiem.ChoDuyet => "nhan-chu-y",
        DongKiem.TuChoi => "nhan-loi",
        _ => "nhan-xam",
    };
}
