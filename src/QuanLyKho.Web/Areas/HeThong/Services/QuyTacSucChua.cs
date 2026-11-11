using System.Globalization;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>
/// Quy tắc sức chứa thuần túy (báo cáo Bảng 2.3 dòng 3 và 4, ký hiệu ở Bảng 1.2), không phụ thuộc CSDL nên kiểm thử được trực tiếp:
/// 1) Điều kiện lọt lòng hộp 3 chiều xoay 6 hướng trực giao: sort(d_b, r_b, c_b) ≤ sort(D_o, R_o, C_o).
/// 2) Điều kiện thể tích và tải trọng: q × t(v) ≤ V(p) - Vđ(p) và q × k(v) ≤ K(p) - Kđ(p).
/// </summary>
public static class QuyTacSucChua
{
    /// <summary>
    /// Kiểm tra 1 đơn vị biến thể kích thước <paramref name="daiBT"/>×<paramref name="rongBT"/>×<paramref name="caoBT"/> (cm)
    /// có đặt lọt vào lòng ô <paramref name="daiO"/>×<paramref name="rongO"/>×<paramref name="caoO"/> (cm) khi cho phép xoay hộp 6 hướng trực giao không.
    /// </summary>
    public static bool VuaKichThuoc3Chieu(
        decimal daiBT, decimal rongBT, decimal caoBT,
        decimal? daiO, decimal? rongO, decimal? caoO)
    {
        if (daiO is null || rongO is null || caoO is null)
            return true;

        Span<decimal> hang = [daiBT, rongBT, caoBT];
        Span<decimal> o = [daiO.Value, rongO.Value, caoO.Value];
        hang.Sort();
        o.Sort();
        return hang[0] <= o[0] && hang[1] <= o[1] && hang[2] <= o[2];
    }

    /// <summary>
    /// Kiểm tra ô <paramref name="o"/> nhận thêm <paramref name="soLuong"/> đơn vị của biến thể có đủ kích thước 3 cạnh
    /// (<paramref name="daiBT"/>, <paramref name="rongBT"/>, <paramref name="caoBT"/> cm) và khối lượng <paramref name="khoiLuongMotDonVi"/> (kg).
    /// </summary>
    public static KetQuaSucChua Tinh(
        ViTri o, decimal daiBT, decimal rongBT, decimal caoBT, decimal khoiLuongMotDonVi, int soLuong,
        decimal theTichDaDung, decimal khoiLuongDaDung)
    {
        if (daiBT <= 0 || rongBT <= 0 || caoBT <= 0 || khoiLuongMotDonVi <= 0)
            return Loi(0, 0, "Kích thước (Dài, Rộng, Cao) và khối lượng biến thể phải lớn hơn 0.", MaLoiDichVu.DuLieuKhongHopLe);
        if (soLuong <= 0)
            return Loi(0, 0, "Số lượng phải lớn hơn 0.", MaLoiDichVu.DuLieuKhongHopLe);
        if (!o.LaO)
            return Loi(0, 0, $"Hàng chỉ được để ở ô, '{o.TenViTri}' là cấp {o.CapViTri}.", MaLoiDichVu.DuLieuKhongHopLe);
        if (!o.DangHoatDong)
            return Loi(0, 0, $"Ô '{o.TenViTri}' đã ngừng hoạt động.", MaLoiDichVu.DuLieuKhongHopLe);
        if (o.DangKhoa)
            return Loi(0, 0, $"Ô '{o.TenViTri}' đang bị khóa để kiểm kê.", MaLoiDichVu.OBiKhoa);

        if (o.LoaiViTri == LoaiViTriGiaTri.TiepNhan)
            return new KetQuaSucChua(true, null, decimal.MaxValue, decimal.MaxValue);

        var theTichCon = (o.TheTichCm3 ?? 0m) - theTichDaDung;
        var khoiCon = (o.KhoiLuongToiDa ?? 0m) - khoiLuongDaDung;

        if (!VuaKichThuoc3Chieu(daiBT, rongBT, caoBT, o.Dai, o.Rong, o.Cao))
        {
            var ktHang = $"{Canh(daiBT)}×{Canh(rongBT)}×{Canh(caoBT)} cm";
            var ktO = $"{Canh(o.Dai ?? 0)}×{Canh(o.Rong ?? 0)}×{Canh(o.Cao ?? 0)} cm";
            return new KetQuaSucChua(
                false, 0, theTichCon, khoiCon,
                $"Kích thước hàng ({ktHang}) vượt lòng ô '{o.TenViTri}' ({ktO}), không đặt vừa.",
                MaLoiDichVu.VuotSucChua);
        }

        return Tinh(o, daiBT * rongBT * caoBT, khoiLuongMotDonVi, soLuong, theTichDaDung, khoiLuongDaDung);
    }

    private static string Canh(decimal v) => v.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',');
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
