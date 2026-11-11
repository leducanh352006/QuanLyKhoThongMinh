using System.Globalization;
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
        if (bt.Dai <= 0 || bt.Rong <= 0 || bt.Cao <= 0 || bt.KhoiLuong <= 0)
            return Tu(false, 0, 0, 0, "Kích thước (Dài, Rộng, Cao) và khối lượng biến thể phải lớn hơn 0.", MaLoiDichVu.DuLieuKhongHopLe);
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

        if (o.Dai is not null && o.Rong is not null && o.Cao is not null)
        {
            Span<decimal> hang = [bt.Dai, bt.Rong, bt.Cao];
            Span<decimal> longO = [o.Dai.Value, o.Rong.Value, o.Cao.Value];
            hang.Sort();
            longO.Sort();
            if (hang[0] > longO[0] || hang[1] > longO[1] || hang[2] > longO[2])
            {
                var ktHang = $"{Canh(bt.Dai)}×{Canh(bt.Rong)}×{Canh(bt.Cao)} cm";
                var ktO = $"{Canh(o.Dai.Value)}×{Canh(o.Rong.Value)}×{Canh(o.Cao.Value)} cm";
                var lyDoKt = $"Kích thước hàng ({ktHang}) vượt lòng ô '{o.TenViTri}' ({ktO}), không đặt vừa.";
                return Tu(false, 0, theTichCon, khoiCon, lyDoKt, MaLoiDichVu.VuotSucChua);
            }
        }

        var theTichMotDonVi = SeedDataSet.TheTichBienThe(bt);
        var toiDaTheoThe = TheoGioiHan(theTichCon, theTichMotDonVi);
        var toiDaTheoKhoi = TheoGioiHan(khoiCon, bt.KhoiLuong);
        var toiDa = Math.Min(toiDaTheoThe, toiDaTheoKhoi);

        if (soLuong <= toiDa)
            return new KetQuaSucChua(true, toiDa, theTichCon, khoiCon);

        var thieuThe = soLuong > toiDaTheoThe;
        var thieuKhoi = soLuong > toiDaTheoKhoi;
        var lyDo = (thieuThe, thieuKhoi) switch
        {
            (true, true) => $"Vượt cả thể tích và khối lượng tối đa của ô '{o.TenViTri}': còn nhận thêm tối đa {toiDa} (cần {soLuong}).",
            (true, false) => $"Vượt thể tích của ô '{o.TenViTri}': còn nhận thêm tối đa {toiDa} (cần {soLuong}).",
            _ => $"Vượt khối lượng tối đa của ô '{o.TenViTri}': còn nhận thêm tối đa {toiDa} (cần {soLuong}).",
        };
        return Tu(false, toiDa, theTichCon, khoiCon, lyDo, MaLoiDichVu.VuotSucChua);
    }

    private static int TheoGioiHan(decimal conLai, decimal motDonVi)
    {
        if (motDonVi <= 0) return int.MaxValue;
        var n = Math.Floor(Math.Max(conLai, 0m) / motDonVi);
        return n >= int.MaxValue ? int.MaxValue : (int)n;
    }

    private static string Canh(decimal v) => v.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',');

    private static KetQuaSucChua Tu(bool du, int? toiDa, decimal theTich, decimal khoi, string lyDo, string maLoi) =>
        new(du, toiDa, theTich, khoi, lyDo, maLoi);
}
