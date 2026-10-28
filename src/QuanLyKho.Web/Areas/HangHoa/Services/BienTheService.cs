using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HangHoa.Services;

/// <summary>CRUD biến thể của một sản phẩm (màn hình Biến thể, việc T3-2). Mã vạch phải duy nhất; kích thước và khối lượng bắt buộc dương.</summary>
public interface IBienTheQuanLy
{
    Task<IReadOnlyList<BienTheDong>> LayTheoSanPham(int maSP, CancellationToken ct = default);

    Task<BienTheDong?> Lay(int maBT, CancellationToken ct = default);

    Task<KetQua> Them(BienTheMoi moi, int maNguoiThucHien, CancellationToken ct = default);

    Task<KetQua> Sua(int maBT, BienTheSuaDoi doi, int maNguoiThucHien, CancellationToken ct = default);
}

public sealed class BienTheService(ISanPhamBienTheStore store, IAuditService audit) : IBienTheQuanLy
{
    public async Task<IReadOnlyList<BienTheDong>> LayTheoSanPham(int maSP, CancellationToken ct = default)
        => (await store.LayBienTheTheoSanPham(maSP, ct)).Select(ChoHienThi).ToList();

    public async Task<BienTheDong?> Lay(int maBT, CancellationToken ct = default)
    {
        var bt = await store.TimBienThe(maBT, ct);
        return bt is null ? null : ChoHienThi(bt);
    }

    public async Task<KetQua> Them(BienTheMoi moi, int maNguoiThucHien, CancellationToken ct = default)
    {
        var sp = await store.TimSanPham(moi.MaSP, ct);
        if (sp is null) return KetQua.Loi("Không tìm thấy sản phẩm.", MaLoiDichVu.KhongTimThay);

        var maVach = moi.MaVach.Trim();
        var thuocTinh = moi.ThuocTinh.Trim();
        var loi = KiemTraMaVach(maVach) ?? KiemTraThuocTinh(thuocTinh) ?? KiemTraKichThuoc(moi.Dai, moi.Rong, moi.Cao, moi.KhoiLuong);
        if (loi is not null) return loi;

        BienThe bt;
        try
        {
            bt = await store.ThemBienThe(new BienThe { MaSP = moi.MaSP, MaVach = maVach, ThuocTinh = thuocTinh, Dai = moi.Dai, Rong = moi.Rong, Cao = moi.Cao, KhoiLuong = moi.KhoiLuong }, ct);
        }
        catch (XungDotDuLieuException)
        {
            return KetQua.Loi("Mã vạch đã tồn tại.", MaLoiDichVu.DuLieuKhongHopLe);
        }

        await audit.Ghi(maNguoiThucHien, "Thêm biến thể", chiTiet: $"{sp.TenSP}: {thuocTinh} ({maVach})", ct: ct);
        return KetQua.Ok($"Đã thêm biến thể '{thuocTinh}'.");
    }

    public async Task<KetQua> Sua(int maBT, BienTheSuaDoi doi, int maNguoiThucHien, CancellationToken ct = default)
    {
        var bt = await store.TimBienThe(maBT, ct);
        if (bt is null) return KetQua.Loi("Không tìm thấy biến thể.", MaLoiDichVu.KhongTimThay);

        var maVach = doi.MaVach.Trim();
        var thuocTinh = doi.ThuocTinh.Trim();
        var loi = KiemTraMaVach(maVach) ?? KiemTraThuocTinh(thuocTinh) ?? KiemTraKichThuoc(doi.Dai, doi.Rong, doi.Cao, doi.KhoiLuong);
        if (loi is not null) return loi;

        if (!string.Equals(maVach, bt.MaVach, StringComparison.OrdinalIgnoreCase)
            && (await store.LayBienTheTheoSanPham(bt.MaSP, ct)).Any(x => x.MaBT != maBT && string.Equals(x.MaVach, maVach, StringComparison.OrdinalIgnoreCase)))
            return KetQua.Loi("Mã vạch đã tồn tại.", MaLoiDichVu.DuLieuKhongHopLe);

        bt.MaVach = maVach;
        bt.ThuocTinh = thuocTinh;
        bt.Dai = doi.Dai;
        bt.Rong = doi.Rong;
        bt.Cao = doi.Cao;
        bt.KhoiLuong = doi.KhoiLuong;
        try
        {
            await store.LuuThayDoi(ct);
        }
        catch (XungDotDuLieuException)
        {
            return KetQua.Loi("Mã vạch đã tồn tại.", MaLoiDichVu.DuLieuKhongHopLe);
        }

        await audit.Ghi(maNguoiThucHien, "Sửa biến thể", chiTiet: thuocTinh, ct: ct);
        return KetQua.Ok("Đã lưu thay đổi.");
    }

    private static KetQua? KiemTraMaVach(string maVach)
        => string.IsNullOrWhiteSpace(maVach) || maVach.Length > 50
            ? KetQua.Loi("Mã vạch không được để trống và tối đa 50 ký tự.", MaLoiDichVu.DuLieuKhongHopLe) : null;

    private static KetQua? KiemTraThuocTinh(string thuocTinh)
        => string.IsNullOrWhiteSpace(thuocTinh) || thuocTinh.Length > 100
            ? KetQua.Loi("Thuộc tính không được để trống và tối đa 100 ký tự.", MaLoiDichVu.DuLieuKhongHopLe) : null;

    private static KetQua? KiemTraKichThuoc(decimal dai, decimal rong, decimal cao, decimal khoiLuong)
        => dai <= 0 || rong <= 0 || cao <= 0 || khoiLuong <= 0
            ? KetQua.Loi("Biến thể phải có đủ dài, rộng, cao và khối lượng, mỗi giá trị lớn hơn 0.", MaLoiDichVu.DuLieuKhongHopLe) : null;

    private static BienTheDong ChoHienThi(BienThe b) => new(b.MaBT, b.MaSP, b.MaVach, b.ThuocTinh, b.Dai, b.Rong, b.Cao, b.KhoiLuong);
}
