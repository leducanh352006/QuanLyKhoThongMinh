using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HangHoa.Services;

/// <summary>
/// Bản thật của <see cref="IHangHoaService"/> (việc T3-3): thay <see cref="Contracts.Stubs.HangHoaServiceStub"/>.
/// Nhóm 2 (Nhập kho) gọi <see cref="TaoLo"/> khi tiếp nhận hàng (Bảng 1.6 dòng 2).
/// </summary>
public sealed class HangHoaService(ILoHangStore loStore, ISanPhamBienTheStore spbt) : IHangHoaService
{
    public async Task<KetQua<LoHangDto>> TaoLo(int maBT, DateOnly? hanDung, string? maVachLo = null, CancellationToken ct = default)
    {
        var bt = await spbt.TimBienThe(maBT, ct);
        if (bt is null)
            return KetQua<LoHangDto>.Loi($"Không tìm thấy biến thể {maBT}.", MaLoiDichVu.KhongTimThay);

        var sp = await spbt.TimSanPham(bt.MaSP, ct);
        if (sp is null)
            return KetQua<LoHangDto>.Loi($"Không tìm thấy sản phẩm của biến thể {maBT}.", MaLoiDichVu.KhongTimThay);

        if (sp.CoHanDung && hanDung is null)
            return KetQua<LoHangDto>.Loi($"Sản phẩm '{sp.TenSP}' quản lý theo hạn dùng: lô phải có hạn dùng.", MaLoiDichVu.DuLieuKhongHopLe);

        var maChiDinh = maVachLo?.Trim();
        if (!string.IsNullOrEmpty(maChiDinh))
        {
            if (maChiDinh.Length > 50)
                return KetQua<LoHangDto>.Loi("Mã lô tối đa 50 ký tự.", MaLoiDichVu.DuLieuKhongHopLe);
            if (await loStore.TimTheoMaVach(maChiDinh, ct) is not null)
                return KetQua<LoHangDto>.Loi($"Mã lô '{maChiDinh}' đã tồn tại; mã lô phải duy nhất.", MaLoiDichVu.DuLieuKhongHopLe);
        }

        // Mã tự sinh hiếm khi đụng độ dưới tải đồng thời; thử lại vài lần trước khi báo lỗi.
        for (var lanThu = 0; lanThu < 5; lanThu++)
        {
            var maThucTe = maChiDinh ?? TaoMaTuDong();
            try
            {
                var lo = await loStore.Them(new LoHang { MaVachLo = maThucTe, MaBT = maBT, HanDung = hanDung }, ct);
                return KetQua<LoHangDto>.Ok(new LoHangDto(lo.MaLo, lo.MaVachLo, lo.MaBT, lo.HanDung));
            }
            catch (XungDotDuLieuException) when (maChiDinh is null)
            {
                // mã tự sinh trùng: thử sinh mã khác ở lần lặp sau
            }
            catch (XungDotDuLieuException)
            {
                return KetQua<LoHangDto>.Loi($"Mã lô '{maChiDinh}' đã tồn tại; mã lô phải duy nhất.", MaLoiDichVu.DuLieuKhongHopLe);
            }
        }

        return KetQua<LoHangDto>.Loi("Không sinh được mã lô duy nhất, thử lại.", MaLoiDichVu.DuLieuKhongHopLe);
    }

    private static string TaoMaTuDong() => $"LO-{DateTime.UtcNow:yyMMdd}-{Random.Shared.Next(100000, 999999)}";
}
