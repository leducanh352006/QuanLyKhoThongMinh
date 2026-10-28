using QuanLyKho.Web.Areas.HangHoa.Models;

namespace QuanLyKho.Web.Areas.HangHoa.Services;

/// <summary>Danh sách lô và hạn dùng, lọc theo biến thể (màn hình Lô hàng, việc T3-3). Chỉ xem: lô được tạo qua <see cref="QuanLyKho.Web.Contracts.IHangHoaService.TaoLo"/>.</summary>
public interface ILoHangQuanLy
{
    Task<IReadOnlyList<LoHangDong>> LayTheoBienThe(int maBT, CancellationToken ct = default);
}

public sealed class LoHangService(ILoHangStore store) : ILoHangQuanLy
{
    public async Task<IReadOnlyList<LoHangDong>> LayTheoBienThe(int maBT, CancellationToken ct = default)
        => (await store.LayTheoBienThe(maBT, ct)).Select(ChoHienThi).ToList();

    private static LoHangDong ChoHienThi(LoHang l) => new(
        l.MaLo, l.MaVachLo, l.MaBT, l.HanDung,
        DaHetHan: l.HanDung is DateOnly h && h < DateOnly.FromDateTime(DateTime.UtcNow));
}
