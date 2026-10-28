using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>
/// Quy tắc sức chứa thuần túy (báo cáo Bảng 2.3 dòng 3 và 4, ký hiệu ở Bảng 1.2), không phụ thuộc CSDL nên kiểm thử được trực tiếp:
/// q × t(v) ≤ V(p) - Vđ(p)  và  q × k(v) ≤ K(p) - Kđ(p).
/// </summary>
public static class QuyTacSucChua
{
    /// <summary>
    /// Kiểm tra ô <paramref name="o"/> nhận thêm <paramref name="soLuong"/> đơn vị của biến thể có thể tích một đơn vị
    /// <paramref name="theTichMotDonVi"/> (cm³) và khối lượng <paramref name="khoiLuongMotDonVi"/> (kg).
    /// <paramref name="theTichDaDung"/> và <paramref name="khoiLuongDaDung"/> là Vđ, Kđ (hàng đang nằm trong ô cộng hàng đã duyệt xếp vào ô nhưng chưa quét xác nhận).
    /// </summary>
    public static KetQuaSucChua Tinh(
        ViTri o, decimal theTichMotDonVi, decimal khoiLuongMotDonVi, int soLuong,
        decimal theTichDaDung, decimal khoiLuongDaDung)
    {
        if (soLuong <= 0)
            return Loi(0, 0, "Số lượng phải lớn hơn 0.", MaLoiDichVu.DuLieuKhongHopLe);
        if (!o.LaO)
            return Loi(0, 0, $"Hàng chỉ được để ở ô, '{o.TenViTri}' là cấp {o.CapViTri}.", MaLoiDichVu.DuLieuKhongHopLe);
        if (!o.DangHoatDong)
            return Loi(0, 0, $"Ô '{o.TenViTri}' đã ngừng hoạt động.", MaLoiDichVu.DuLieuKhongHopLe);
        if (o.DangKhoa)
            return Loi(0, 0, $"Ô '{o.TenViTri}' đang bị khóa để kiểm kê.", MaLoiDichVu.OBiKhoa);

        // Ô Tiếp nhận không giới hạn kích thước.
        if (o.LoaiViTri == LoaiViTriGiaTri.TiepNhan)
            return new KetQuaSucChua(true, null, decimal.MaxValue, decimal.MaxValue);

        var theTichCon = (o.TheTichCm3 ?? 0m) - theTichDaDung;
        var khoiCon = (o.KhoiLuongToiDa ?? 0m) - khoiLuongDaDung;

        var toiDaTheoThe = TheoGioiHan(theTichCon, theTichMotDonVi);
        var toiDaTheoKhoi = TheoGioiHan(khoiCon, khoiLuongMotDonVi);
        var toiDa = Math.Min(toiDaTheoThe, toiDaTheoKhoi);

        if (soLuong <= toiDa)
            return new KetQuaSucChua(true, toiDa, theTichCon, khoiCon);

        // Nêu rõ thiếu thể tích hay thiếu khối lượng; thiếu cả hai thì nói cả hai.
        var thieuThe = soLuong > toiDaTheoThe;
        var thieuKhoi = soLuong > toiDaTheoKhoi;
        var lyDo = (thieuThe, thieuKhoi) switch
        {
            (true, true) => $"Vượt cả thể tích và khối lượng tối đa của ô '{o.TenViTri}': còn nhận thêm tối đa {toiDa} (cần {soLuong}).",
            (true, false) => $"Vượt thể tích của ô '{o.TenViTri}': còn nhận thêm tối đa {toiDa} (cần {soLuong}).",
            _ => $"Vượt khối lượng tối đa của ô '{o.TenViTri}': còn nhận thêm tối đa {toiDa} (cần {soLuong}).",
        };
        return new KetQuaSucChua(false, toiDa, theTichCon, khoiCon, lyDo, MaLoiDichVu.VuotSucChua);
    }

    private static int TheoGioiHan(decimal conLai, decimal motDonVi)
    {
        if (motDonVi <= 0) return int.MaxValue;
        var n = Math.Floor(Math.Max(conLai, 0m) / motDonVi);
        return n >= int.MaxValue ? int.MaxValue : (int)n;
    }

    private static KetQuaSucChua Loi(decimal theTich, decimal khoi, string lyDo, string maLoi) =>
        new(false, 0, theTich, khoi, lyDo, maLoi);
}
