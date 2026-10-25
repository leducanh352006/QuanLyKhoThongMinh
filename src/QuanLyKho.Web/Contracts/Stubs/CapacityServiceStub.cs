using QuanLyKho.Web.SeedData;

namespace QuanLyKho.Web.Contracts.Stubs;

/// <summary>
/// Bản giả của <see cref="ICapacityService"/>: tính thể tích và khối lượng đã dùng từ tồn hiện có của <see cref="IStockService"/>
/// và kích thước trong SeedData. Chỉ biết các lô và biến thể có trong SeedData (lô tạo mới ở bản giả HangHoa không được tính).
/// </summary>
public sealed class CapacityServiceStub(IStockService stock, ILockService khoa) : ICapacityService
{
    public async Task<KetQuaSucChua> KiemTra(int maViTri, int maBT, int soLuong, CancellationToken ct = default)
    {
        if (soLuong <= 0)
            return Tu(false, null, 0, 0, "Số lượng phải lớn hơn 0.", MaLoiDichVu.DuLieuKhongHopLe);

        var o = SeedDataSet.TimViTri(maViTri);
        var bt = SeedDataSet.TimBienThe(maBT);
        if (o is null) return Tu(false, null, 0, 0, $"Không tìm thấy vị trí {maViTri}.", MaLoiDichVu.KhongTimThay);
        if (bt is null) return Tu(false, null, 0, 0, $"Không tìm thấy biến thể {maBT}.", MaLoiDichVu.KhongTimThay);
        if (o.CapViTri != SeedDataSet.CapO)
            return Tu(false, null, 0, 0, $"Hàng chỉ được để ở ô, '{o.TenViTri}' là cấp {o.CapViTri}.", MaLoiDichVu.DuLieuKhongHopLe);

        if (await khoa.DangKhoa(maViTri, ct))
            return Tu(false, 0, 0, 0, $"Ô '{o.TenViTri}' đang bị khóa để kiểm kê.", MaLoiDichVu.OBiKhoa);

        // Ô Tiếp nhận không giới hạn kích thước.
        if (o.LoaiViTri == SeedDataSet.OTiepNhan)
            return new KetQuaSucChua(true, null, decimal.MaxValue, decimal.MaxValue);

        var tong = SeedDataSet.TheTichO(o) ?? 0m;
        var khoiToiDa = o.KhoiLuongToiDa ?? 0m;

        decimal theTichDaDung = 0, khoiDaDung = 0;
        foreach (var t in await stock.LayTonTheoViTri(maViTri, ct))
        {
            var lo = SeedDataSet.TimLo(t.MaLo);
            var b = lo is null ? null : SeedDataSet.TimBienThe(lo.MaBT);
            if (b is null) continue;
            theTichDaDung += t.SoLuongTon * SeedDataSet.TheTichBienThe(b);
            khoiDaDung += t.SoLuongTon * b.KhoiLuong;
        }

        var theTichCon = tong - theTichDaDung;
        var khoiCon = khoiToiDa - khoiDaDung;
        var theTichMotDonVi = SeedDataSet.TheTichBienThe(bt);

        var toiDaTheoThe = theTichMotDonVi > 0 ? (int)Math.Floor(Math.Max(theTichCon, 0) / theTichMotDonVi) : int.MaxValue;
        var toiDaTheoKhoi = bt.KhoiLuong > 0 ? (int)Math.Floor(Math.Max(khoiCon, 0) / bt.KhoiLuong) : int.MaxValue;
        var toiDa = Math.Min(toiDaTheoThe, toiDaTheoKhoi);

        if (soLuong <= toiDa)
            return new KetQuaSucChua(true, toiDa, theTichCon, khoiCon);

        var lyDo = toiDaTheoKhoi < toiDaTheoThe
            ? $"Vượt khối lượng tối đa của ô '{o.TenViTri}': còn nhận thêm tối đa {toiDa} (cần {soLuong})."
            : $"Vượt thể tích của ô '{o.TenViTri}': còn nhận thêm tối đa {toiDa} (cần {soLuong}).";
        return Tu(false, toiDa, theTichCon, khoiCon, lyDo, MaLoiDichVu.VuotSucChua);
    }

    private static KetQuaSucChua Tu(bool du, int? toiDa, decimal theTich, decimal khoi, string lyDo, string maLoi) =>
        new(du, toiDa, theTich, khoi, lyDo, maLoi);
}
